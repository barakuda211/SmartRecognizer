﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO.Ports;

using System.IO;

namespace AForge.WindowsForms
{
    delegate void FormUpdateDelegate();

    public partial class MainForm : Form
    {
        /// <summary>
        /// Класс, реализующий всю логику работы
        /// </summary>
        private Controller controller = null;

        /// <summary>
        /// Событие для синхронизации таймера
        /// </summary>
        private AutoResetEvent evnt = new AutoResetEvent(false);
                
        /// <summary>
        /// Список устройств для снятия видео (веб-камер)
        /// </summary>
        private FilterInfoCollection videoDevicesList;
        
        /// <summary>
        /// Выбранное устройство для видео
        /// </summary>
        private IVideoSource videoSource;
        
        /// <summary>
        /// Таймер для измерения производительности (времени на обработку кадра)
        /// </summary>
        private Stopwatch sw = new Stopwatch();
        
        /// <summary>
        /// Таймер для обновления объектов интерфейса
        /// </summary>
        System.Threading.Timer updateTmr;

        /// <summary>
        /// Функция обновления формы, тут же происходит анализ текущего этапа, и при необходимости переключение на следующий
        /// Вызывается автоматически - это плохо, надо по делегатам вообще-то
        /// </summary>
        private void UpdateFormFields()
        {
            //  Проверяем, вызвана ли функция из потока главной формы. Если нет - вызов через Invoke
            //  для синхронизации, и выход
            if (statusLabel.InvokeRequired)
            {
                this.Invoke(new FormUpdateDelegate(UpdateFormFields));
                return;
            }

            sw.Stop();
            originalImageBox.Image = controller.GetOriginalImage();
            processedImgBox.Image = controller.GetProcessedImage();
        }

        /// <summary>
        /// Обёртка для обновления формы - перерисовки картинок, изменения состояния и прочего
        /// </summary>
        /// <param name="StateInfo"></param>
        public void Tick(object StateInfo)
        {
            UpdateFormFields();
            return;
        }

        string main_path = Environment.CurrentDirectory+"\\Morze";
        string real_ex_path = Environment.CurrentDirectory + "\\MorzeRealExamples";
        BaseNetwork net = null;
        LetterType recognizedClass;

        private bool isCameraSelected = false;

        static int symbolsCount = 10;
        private double max_error = 0.0005;
        private int epoches = 100;
        private List<ProgressBar> bars = new List<ProgressBar>();

        public MainForm()
        {
            InitializeComponent();
            // Список камер получаем
            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
            {
                cmbVideoSource.Items.Add(videoDevice.Name);
            }
            controller = new Controller(new FormUpdateDelegate(UpdateFormFields));
            controller.settings.processImg = true;
            comboBox_net.Items.Add("Accord Net");
            comboBox_net.Items.Add("My Net");
            comboBox_net.SelectedItem = "Accord Net";
            LetterType temp = 0;
            for (int i = 0; i < 10; i++)
            {
                var lb = new Label(); lb.Text = ((LetterType)(i)).ToString();
                lb.Parent = tableLayoutPanel1; lb.Dock = DockStyle.Fill;
                tableLayoutPanel1.Controls.Add(lb, 0, i);
                var pb = new ProgressBar(); pb.Dock = DockStyle.Fill;
                pb.Maximum = 100; pb.Minimum = 0;
                tableLayoutPanel1.Controls.Add(pb, 1, i);
                bars.Add(pb);
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //  Время засекаем
            sw.Restart();

            //  Отправляем изображение на обработку, и выводим оригинал (с раскраской) и разрезанные изображения
            if(controller.Ready)
                
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                controller.ProcessImage((Bitmap)eventArgs.Frame.Clone());
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isCameraSelected)
                return;
            if (videoSource == null)
            {
                var vcd = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
                vcd.VideoResolution = vcd.VideoCapabilities[resolutionsBox.SelectedIndex];
                Debug.WriteLine(vcd.VideoCapabilities[1].FrameSize.ToString());
                Debug.WriteLine(resolutionsBox.SelectedIndex);
                videoSource = vcd;
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                videoSource.Start();
                StartButton.Text = "Стоп";
                cmbVideoSource.Enabled = false;
            }
            else
            {
                videoSource.SignalToStop();
                if (videoSource != null && videoSource.IsRunning && originalImageBox.Image != null)
                {
                    originalImageBox.Image.Dispose();
                }
                videoSource = null;
                StartButton.Text = "Старт";
                cmbVideoSource.Enabled = true;
            }
        }

        private void tresholdTrackBar_ValueChanged(object sender, EventArgs e)
        {
            controller.settings.threshold = (byte)tresholdTrackBar.Value;
            controller.settings.differenceLim = (float)tresholdTrackBar.Value/tresholdTrackBar.Maximum;
        }

        private void borderTrackBar_ValueChanged(object sender, EventArgs e)
        {
            controller.settings.border = borderTrackBar.Value;
        }

        private void marginTrackBar_ValueChanged(object sender, EventArgs e)
        {
            controller.settings.margin = marginTrackBar.Value;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (updateTmr != null)
                updateTmr.Dispose();

            //  Как-то надо ещё робота подождать, если он работает

            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }


        private void cmbVideoSource_SelectionChangeCommitted(object sender, EventArgs e)
        {
            var vcd = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
            resolutionsBox.Items.Clear();
            for (int i = 0; i < vcd.VideoCapabilities.Length; i++)
                resolutionsBox.Items.Add(vcd.VideoCapabilities[i].FrameSize.ToString());
            resolutionsBox.SelectedIndex = 0;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            controller.settings.processImg = checkBox1.Checked;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            controller.settings.border = borderTrackBar.Value;
        }

        private double[] imgToData(Bitmap img)
        {
            double[] res = new double[img.Width];

            for (int i = 0; i < img.Width; i++)
            {
                double sum = 0;
                for (int j = 0; j < img.Height; j++)
                {
                    var r = img.GetPixel(i, j).R;
                    sum += 1 - r* 1.0 / 255;
                    //sum += r;
                }
                res[i] = sum;
            }
            return res;
        }

        /// <summary>
        /// Обучение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (Directory.Exists("Morze"))
                Directory.Delete("Morze", true);
            MorzeGenerator.MakeExamples(100, int.Parse(textBox_countExamples.Text));

            SamplesSet samples = new SamplesSet();
            Sample newSample;

            int type = 0;

            foreach (var directory in Directory.GetDirectories(main_path))
            {
                if (type >= symbolsCount)
                    break;

                foreach (var file in Directory.GetFiles(directory))
                {
                    var img = new Bitmap(file);
                    newSample = new Sample(imgToData(img), symbolsCount, (LetterType)type);
                    samples.AddSample(newSample);
                }
                type++;
            }
            type = 0;
            foreach (var directory in Directory.GetDirectories(real_ex_path))
            {
                if (type >= symbolsCount)
                    break;

                foreach (var file in Directory.GetFiles(directory))
                {
                    var img = new Bitmap(file);
                    newSample = new Sample(imgToData(img), symbolsCount, (LetterType)type);
                    samples.AddSample(newSample);
                }
                type++;
            }
            var acc = net.TrainOnDataSet(samples, epoches, max_error, true);

            outputLabel.Text = "Точность: " + acc/100.0;
        }

        /// <summary>
        /// Создание сети
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            int[] structure = netStructureBox.Text.Split(';').Select((c) => int.Parse(c)).ToArray();

            if (structure.Length < 2 || structure[structure.Length - 1] != symbolsCount)
            {
                MessageBox.Show("А давайте вы структуру сети нормально запишите, ОК?", "Ошибка", MessageBoxButtons.OK);
                return;
            }

            var s = (string)comboBox_net.Text;
            if (s == "Accord Net")
                net = new AccordNet(structure);
            else
                net = new NeuralNetwork(structure);
            epoches = (int)EpochesCounter.Value;
        }

        private void EpochesCounter_ValueChanged(object sender, EventArgs e)
        {
            epoches = (int)EpochesCounter.Value;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse((sender as TextBox).Text, out double r))
            {
                max_error = r;
            }
        }

        /// <summary>
        /// Распознать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            recognise();
        }

        private void recognise()
        {
            //var img = Imaging.UnmanagedImage.FromManagedImage(new Bitmap(processedImgBox.Image));
            var img = new Bitmap(processedImgBox.Image);
            Sample currentImage = new Sample(imgToData(img), symbolsCount);
            recognizedClass = net.Predict(currentImage);

            //WriteOutput(recognizedClass.ToString());
            SetRecogResult(outputLabel, recognizedClass.ToString(),net.getOutput());
        }

        private void test(bool new_dataset = true)
        {
            if (new_dataset)
            {
                if (Directory.Exists("Morze"))
                    Directory.Delete("Morze", true);
                MorzeGenerator.MakeExamples(100, int.Parse(textBox_test.Text));
            }
            SamplesSet samples = new SamplesSet();
            Sample newSample;int type = 0;
            foreach (var directory in Directory.GetDirectories(main_path))
            {
                if (type >= symbolsCount)
                    break;

                foreach (var file in Directory.GetFiles(directory))
                {
                    //var img = AForge.Imaging.UnmanagedImage.FromManagedImage(new Bitmap(file));
                    var img = new Bitmap(file);
                    newSample = new Sample(imgToData(img), symbolsCount, (LetterType)type);
                    samples.AddSample(newSample);
                }
                type++;
            }
            outputLabel.Text = "Процент правильного распознавания: " + net.TestOnDataSet(samples)*100;
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            test(true);
        }

        private void processedImgBox_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void netStructureBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void borderTrackBar_Scroll(object sender, EventArgs e)
        {

        }

        private void originalImageBox_Click(object sender, EventArgs e)
        {

        }

        private void cmbVideoSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            isCameraSelected = true;
        }

        private void checkBox_recog_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_recog.Checked)
            {
                Thread myThread = new Thread(new ThreadStart(AutoRecognition));
                myThread.Start(); // запускаем поток
            }
        }

        private void SetRecogResult(Control ctrl, string text, double[] res)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetRecogResult(ctrl, text, res)));
                return;
            }

            ctrl.Text = text;
            for (int i = 0; i < 10; i++)
                bars[i].Value = (int)(res[i] * 100);
            tableLayoutPanel1.Refresh();
        }
        private void AutoRecognition()
        {
            while (true)
            {
                if (!checkBox_recog.Checked)
                    return;
                Thread.Sleep(1000);
                recognise();
            }
        }

        private void button_saveExample_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists("MorzeRealExamples"))
                Directory.CreateDirectory("MorzeRealExamples");
            LetterType f = 0;
            for (int i =0; i < symbolsCount; i++)
            {
                if (!Directory.Exists("MorzeRealExamples\\"+f.ToString()))
                    Directory.CreateDirectory("MorzeRealExamples\\" + f.ToString());
                f++;
            }
            LetterType current_letter=0;
            switch (textBox_exmapleCode.Text[0])
            {
                case 'а': current_letter = (LetterType)0; break;
                case 'б': current_letter = (LetterType)1; break;
                case 'в': current_letter = (LetterType)2; break;
                case 'г': current_letter = (LetterType)3; break;
                case 'д': current_letter = (LetterType)4; break;
                case 'е': current_letter = (LetterType)5; break;
                case 'ж': current_letter = (LetterType)6; break;
                case 'з': current_letter = (LetterType)7; break;
                case 'и': current_letter = (LetterType)8; break;
                case 'к': current_letter = (LetterType)9; break;
            }
            var lst = Directory.GetFiles("MorzeRealExamples\\" + current_letter.ToString());
            processedImgBox.Image.Save("MorzeRealExamples\\" + current_letter.ToString() + "\\" + lst.Length + ".png", System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
