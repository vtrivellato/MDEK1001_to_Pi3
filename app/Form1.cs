using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Drawing.Imaging;

namespace RTLSDataObserver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1024, 666);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var dadosBase = new
                {
                    x = new List<decimal> { 1, 2, 3, 4, 5, 6, 6, 7, 7, 8, 8, 8, 8, 8 },
                    y = new List<decimal> { 1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5 },
                    grid_size = 0.2m,
                    h = 8
                };

                var dados = JsonSerializer.Serialize(dadosBase, new JsonSerializerOptions()
                {
                    // WhenWritingNull      -> somente vari�veis nulas
                    // WhenWritingDefault   -> valor default da vari�vel
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    WriteIndented = true
                });

                var heatmapBase64 = string.Empty;

                using (var client = new HttpClient { BaseAddress = new Uri("https://fg2d4mx1j6.execute-api.us-east-1.amazonaws.com") })
                {
                    client.Timeout = new TimeSpan(0, 0, 30);

                    using (var content = new StringContent(dados, Encoding.UTF8, "application/json"))
                    {
                        heatmapBase64 = client.PostAsync("/test/heatmaps", content).Result.Content.ReadAsStringAsync().Result;
                    }
                }

                if (string.IsNullOrWhiteSpace(heatmapBase64))
                {
                    MessageBox.Show("N�o foi poss�vel gerar a imagem", "Erro ao gerar mapa de calor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(heatmapBase64)))
                {
                    using (Bitmap bmp = new Bitmap(ms))
                    {
                        pictureBox1.Image = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.DontCare);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exce��o lan�ada ao gerar mapa de calor" + Environment.NewLine + ex.Message, "Erro ao gerar mapa de calor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}