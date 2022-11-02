using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Drawing.Imaging;
using MongoDB.Driver;
using System.Windows.Forms;

namespace RTLSDataObserver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<PositionRegister> positionRegisterList;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1024, 666);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            try
            {
                var mongoClient = new MongoClient("mongodb+srv://ftt-tcc-rtls:xIaXng8NwfkTGQYn@cluster0.w8pmvla.mongodb.net/?retryWrites=true&w=majority");

                var db = mongoClient.GetDatabase("ftt-tcc-rtls");
                var positionsCollection = db.GetCollection<PositionRegister>("PositionRegister");

                var filterBuilder = Builders<PositionRegister>.Filter;
                var filter = filterBuilder.Empty;

                positionRegisterList = positionsCollection.Find(filter).ToList();
                positionRegisterList = positionRegisterList.OrderBy(x => x.INS_DATE).ToList();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                var minDate = positionRegisterList.Select(x => x.INS_DATE.Date).Min();
                var maxDate = positionRegisterList.Select(x => x.INS_DATE.Date).Max();

                dateTimePicker1.MinDate = minDate;
                dateTimePicker1.MaxDate = maxDate;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            pictureBox1.Image = null;

            var filteredData = positionRegisterList.Where(x => x.INS_DATE.Date == dateTimePicker1.Value.Date).Select(r => new { x = r.POS_X, y = r.POS_Y});

            var xCoordinates = filteredData.Select(x => x.x).ToList();
            var yCoordinates = filteredData.Select(y => y.y).ToList().ToList();

            await GenerateHeatMap(xCoordinates, yCoordinates);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            pictureBox1.Image = null;

            var filteredData = positionRegisterList.Select(r => new { x = r.POS_X, y = r.POS_Y });

            var xCoordinates = filteredData.Select(x => x.x).ToList();
            var yCoordinates = filteredData.Select(y => y.y).ToList().ToList();

            await GenerateHeatMap(xCoordinates, yCoordinates);
        }

        private async Task GenerateHeatMap(List<decimal> xCoordinates, List<decimal> yCoordinates)
        {
            try
            {
                var dadosBase = new
                {
                    x = xCoordinates,
                    y = yCoordinates
                };

                var dados = JsonSerializer.Serialize(dadosBase, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    WriteIndented = true
                });

                var heatmapBase64 = string.Empty;

                using (var client = new HttpClient { BaseAddress = new Uri("https://fg2d4mx1j6.execute-api.us-east-1.amazonaws.com/test/heatmaps/") })
                {
                    client.Timeout = new TimeSpan(0, 0, 30);

                    using var content = new StringContent(dados, Encoding.UTF8, "application/json");

                    var returnData = await client.PostAsync("/test/heatmaps", content);
                    heatmapBase64 = returnData.Content.ReadAsStringAsync().Result;
                }

                if (string.IsNullOrWhiteSpace(heatmapBase64))
                {
                    MessageBox.Show("Não foi possível gerar a imagem", "Erro ao gerar mapa de calor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var ms = new MemoryStream(Convert.FromBase64String(heatmapBase64));
                using var bmp = new Bitmap(ms);

                pictureBox1.Image = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.DontCare);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exceção lançada ao gerar mapa de calor" + Environment.NewLine + ex.Message, "Erro ao gerar mapa de calor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                pictureBox2.Visible = false;
            }
        }
    }
}