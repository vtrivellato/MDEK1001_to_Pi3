using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using System.Data;
using System.Reflection;

namespace worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Replication interface start time: {time}", DateTimeOffset.Now);

            await ReplicateData();
            
            _logger.LogInformation("Replication interface finish time: {time}", DateTimeOffset.Now);

            await Task.Delay(60000, stoppingToken);
        }
    }

    static string sqlSelect = 
        @"
            SELECT *
            FROM pos_register 
            WHERE sent = 0
                AND pos_x != 'nan'
            ORDER BY 1
            LIMIT 1000
        ";

    static string sqlUpdate = 
        @"
            UPDATE pos_register 
            SET sent = 1
            WHERE ID = @id
        ";

    private async Task ReplicateData()
    {
        _logger.LogInformation("Gathering data from origin DB...");
        var positionsList = await LoadDataOriginDB();
        _logger.LogInformation("{0} register(s) loaded", positionsList.Count);

        _logger.LogInformation("Replicating data to destination DB...");        
        await SendDataDestinationDB(positionsList);
        _logger.LogInformation("Data replication finished");       
    }

    #region SQLite

    private async Task<List<PositionRegister>> LoadDataOriginDB()
    {
        var queryResult = new List<PositionRegister>();

        using (var connection = new SqliteConnection("Data Source=..\\rasp\\test.db"))
        {
            connection.Open();

            if (connection.State == ConnectionState.Closed)
            {
                return null;
            }

            queryResult = await ExecuteQueryList<PositionRegister>(connection, sqlSelect);
        
            connection.Close();
        }
        
        return queryResult;
    }

    private async Task<bool> AtualizaDadosSQLite(SqliteConnection connection, PositionRegister posregister)
    {       
        try
        {
            var command = connection.CreateCommand();

            command.CommandText = sqlUpdate;

            command.Parameters.AddWithValue("@id", posregister.ID);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception)
        {
            return false;
        }
            
        return true;
    }

    #endregion

    #region MongoDB
    private async Task SendDataDestinationDB(List<PositionRegister> positionsList)
    {
        try
        {
            using (var connection = new SqliteConnection("Data Source=..\\rasp\\test.db"))
            {
                connection.Open();

                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                var mongoClient = new MongoClient("mongodb+srv://ftt-tcc-rtls:xIaXng8NwfkTGQYn@cluster0.w8pmvla.mongodb.net/?retryWrites=true&w=majority");

                var positionsCollection = mongoClient.GetDatabase("ftt-tcc-rtls").GetCollection<PositionRegister>("PositionRegister");

                foreach (var posRegister in positionsList)
                {
                    var auxPosRegister = await positionsCollection.Find(x => x.ID == posRegister.ID)
                                                                  .FirstOrDefaultAsync();

                    if (auxPosRegister == null)
                    {
                        positionsCollection.InsertOne(posRegister);   
                    }
  
                    await AtualizaDadosSQLite(connection, posRegister);           
                }                
        
                connection.Close();
            }
        }
        catch (Exception)
        {            
            throw;
        }        
    }

    #endregion

    #region Query execution

    private async Task<T> ExecuteQuery<T>(SqliteConnection connection, string query)
    {
        T retorno = default;

        using (var dbCommand = connection.CreateCommand())
        {
            dbCommand.CommandText = query;
            dbCommand.CommandType = CommandType.Text;

            using (var reader = await dbCommand.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    retorno = DataReaderMapObject<T>(reader);
                    await reader.NextResultAsync();
                }
            }
        }

        return retorno;
    }

    private async Task<List<T>> ExecuteQueryList<T>(SqliteConnection connection, string query, int timeout = 240)
    {
        List<T> list = new();

        try
        {
            using (var dbCommand = connection.CreateCommand())
            {
                dbCommand.CommandText = query;
                dbCommand.CommandTimeout = timeout;

                using (var reader = await dbCommand.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        list = DataReaderMapToList<T>(reader);
                        await reader.NextResultAsync();
                    }
                }
            }
        }
        catch (Exception)
        {
            return new List<T>();
        }

        return list;
    }

    #endregion

    #region Data mapper

    private static T DataReaderMap<T>(IDataReader dr)
    {
        var nameProp = string.Empty;

        try
        {
            var colunas = dr.GetSchemaTable()!
                          .Rows
                          .OfType<DataRow>()
                          .Select(row => row["ColumnName"]
                                         .ToString()
                                         .ToUpper()
                          );

            T obj = default;

            obj = Activator.CreateInstance<T>();

            var props = obj.GetType().GetProperties();

            foreach (PropertyInfo prop in props)
            {
                nameProp = prop.Name;

                if (colunas.Contains(prop.Name.ToUpper()))
                {
                    if (!object.Equals(dr[prop.Name], DBNull.Value))
                    {
                        try
                        {
                            switch (prop.PropertyType.Name)
                            {
                                case "String":
                                    prop.SetValue(obj, (string)dr[prop.Name], null);
                                    break;
                                case "Int32":
                                    prop.SetValue(obj, Convert.ToInt32(dr[prop.Name]), null);
                                    break;
                                case "Int64":
                                case "Double":
                                case "Decimal":
                                    prop.SetValue(obj, Convert.ToDecimal(dr[prop.Name]), null);
                                    break;
                                case "DateTime":
                                    prop.SetValue(obj, Convert.ToDateTime(dr[prop.Name]), null);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch
                        {
                            prop.SetValue(obj, Convert.ToDecimal(dr[prop.Name]), null);
                        }                        
                    }
                }
                else
                {
                    prop.SetValue(obj, default, null);
                }
            }

            return obj;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao Mapear dado Coluna banco x class, coluna: {nameProp}", ex);
        }
    }

    private static T DataReaderMapObject<T>(IDataReader dr)
    {
        try
        {
            return DataReaderMap<T>(dr);
        }
        catch (Exception ex)
        {
            return default;
            throw new Exception($"Erro ao Mapear objeto: {typeof(T).Name}", ex);
        }
    }

    private static List<T> DataReaderMapToList<T>(IDataReader dr)
    {
        List<T> list = new();

        try
        {
            while (dr.Read())
            {
                T item = DataReaderMapObject<T>(dr);

                if (item == null || item.Equals(default(T)))
                {
                    continue;
                }

                list.Add(item!);
            }

            return list;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao Mapear lista de objetos: {typeof(T).Name}", ex);
        }
    }

    #endregion
}
