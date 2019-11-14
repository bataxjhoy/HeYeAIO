using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
namespace HeYeAIO
{
    public partial class MainForm : Form
    {
        public static string receiveMessage; //消息
        private Thread myThread;//计数线程
        public MainForm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 窗口加载，数据初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {   //按键
            StartBtn.Text = "启动";
            StartBtn.BackColor = Color.Green;
           //禁止跨线程访问检测
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            logoBox.Image = Image.FromFile(@"..\bata_logo.png");
            try
            {
                SerialServer.SerialPort_init();//初始化SerialPort对象 并打开
                comboPortName.Text=SerialServer.R485Port.PortName;//显示串口号                
            }
            catch 
            {   
                MessageBox.Show("未发现串口！");
                Application.Exit();
            }
            data_Init();//从文件读取数据 ，并下发电路板
        }
        private void countThread()
        {
            while (true)
            {
                if (receiveMessage != "" && receiveMessage != null)
                {
                    if (receiveMessage == "run over a sponge")//跑完一张，计数减
                    {
                        if (Global.bed_num > 0)
                        {
                            Global.bed_num -= 1;
                            SerialServer.send_BedNum((short)(Global.bed_num));

                            bedNumLabel.BeginInvoke(new Action(() =>
                            {
                                bedNumLabel.Text = Global.bed_num.ToString();//显示张数，计数
                            }));
                            
                        }
                        if (info_textBox.Lines.Length >= 5) info_textBox.Text = "";
                        info_textBox.Text += "完成一张棉" + "  " + DateTime.Now + "\r\n";//显示完成时间                    
                    }
                    receiveMessage = "";
                    if (Global.bed_num == 0)
                    {                        
                        StartBtn.BeginInvoke(new Action(() =>
                        {
                            StartBtn_Click(null, null);//停止流水线
                            MessageBox.Show("计数为零，请从新设置张数！");
                        }));
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }


        //private void timer1_Tick(object sender, EventArgs e)//定时器接收消息
        //{
        //    if (receiveMessage != "" && receiveMessage != null)
        //    {
        //        if (receiveMessage == "run over a sponge")//跑完一张，计数减
        //        {
        //            if (Global.bed_num > 0)
        //            {
        //                Global.bed_num -= 1;
        //                SerialServer.send_BedNum((short)(Global.bed_num));
        //            }
        //            if (info_textBox.Lines.Length >= 5) info_textBox.Text = "";
        //            info_textBox.Text += "完成一张棉" + "  " + DateTime.Now + "\r\n";//显示完成时间                    
        //        }
        //        receiveMessage = "";

        //        bedNumLabel.Text = Global.bed_num.ToString();//显示张数，计数
        //        if (Global.bed_num == 0)
        //        {
        //            StartBtn_Click(null, null);//停止流水线
        //        }
        //    }
        //    else
        //    {
        //        Thread.Sleep(10);
        //    }
        //}
        /// <summary>
        /// 窗口关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Global.my_start == 1)//机器人运行中
            {
                DialogResult result = MessageBox.Show("机器正在运行，确定关闭？", "系统提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    SerialServer.send_start(0x00);
                    myThread.Abort();
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            if (SerialServer.R485Port.IsOpen) SerialServer.R485Port.Close(); //关闭串口  
        }
     

        private void data_Init()//从文件读取数据，并下发电路板
        {
            Global.savebuf = FileOperation.ReadByteFile("config.txt", Global.savebuf);
            Global.col_data_high = Global.savebuf[0];//列切刀
            Global.col_data_low = Global.savebuf[1];
            Global.row_num = Global.savebuf[2];//排数
            Global.bord_length = Global.savebuf[3];  //前边距
            Global.bed_num = (short)(Global.savebuf[4] << 8 | Global.savebuf[5]);//张数
            Global.work_mode=Global.savebuf[6];//工作模式
            for (int i = 0; i < 13; i++)//0-12电磁阀的胶量
            {
                Global.glueNum[i] = Global.savebuf[i+10];
            }

            //下发电路板各状态
            SerialServer.send_ColState(Global.col_data_high, Global.col_data_low);//列切刀
            SerialServer.send_RowNum(Global.row_num);//排数
            SerialServer.send_length(Global.bord_length);//前边距
            SerialServer.send_BedNum((short)Global.bed_num);//张数
            SerialServer.send_work_mode(Global.work_mode);//工作模式
            for (int i = 0; i < 13; i++)
            {
                SerialServer.send_glue_num((byte)i, Global.glueNum[i]);//0-12电磁阀的胶量
            }


            //更新列切刀使能位显示
            if ((Global.col_data_low & 0x01) != 0) Col_1_Btn.BackColor = Color.Green;
            else Col_1_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x02) != 0) Col_2_Btn.BackColor = Color.Green;
            else Col_2_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x04) != 0) Col_3_Btn.BackColor = Color.Green;
            else Col_3_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x08) != 0) Col_4_Btn.BackColor = Color.Green;
            else Col_4_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x10) != 0) Col_5_Btn.BackColor = Color.Green;
            else Col_5_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x20) != 0) Col_6_Btn.BackColor = Color.Green;
            else Col_6_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x40) != 0) Col_7_Btn.BackColor = Color.Green;
            else Col_7_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_low & 0x80) != 0) Col_8_Btn.BackColor = Color.Green;
            else Col_8_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_high & 0x01) != 0) Col_9_Btn.BackColor = Color.Green;
            else Col_9_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_high & 0x02) != 0) Col_10_Btn.BackColor = Color.Green;
            else Col_10_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_high & 0x04) != 0) Col_11_Btn.BackColor = Color.Green;
            else Col_11_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_high & 0x08) != 0) Col_12_Btn.BackColor = Color.Green;
            else Col_12_Btn.BackColor = Color.DarkGray;
            if ((Global.col_data_high & 0x10) != 0) Col_13_Btn.BackColor = Color.Green;
            else Col_13_Btn.BackColor = Color.DarkGray;
            //前边距显示更新
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
            if (Global.bord_length == 0) boradLenth0Btn.BackColor = Color.Green;
            else if (Global.bord_length == 1) boradLenth1Btn.BackColor = Color.Green;
            else if (Global.bord_length == 2) boradLenth2Btn.BackColor = Color.Green;
            else if (Global.bord_length == 3) boradLenth3Btn.BackColor = Color.Green;
            else if (Global.bord_length == 4) boradLenth4Btn.BackColor = Color.Green;
            else if (Global.bord_length == 5) boradLenth5Btn.BackColor = Color.Green;
            else if (Global.bord_length == 6) boradLenth6Btn.BackColor = Color.Green;
            else if (Global.bord_length == 7) boradLenth7Btn.BackColor = Color.Green;
            //更新排数
            rowNumLabel.Text = Global.row_num.ToString();
            //更新张数
            bedNumLabel.Text = Global.bed_num.ToString();
            //更新工作模式
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
            if (Global.work_mode == 0) holeAndGlueAndMagnetBtn.BackColor = Color.Green;
            else if (Global.work_mode == 1) glueAndMagnetBtn.BackColor = Color.Green;
            else if (Global.work_mode == 2) onlyGlueBtn.BackColor = Color.Green;
            else if (Global.work_mode == 3) onlyMagnetBtn.BackColor = Color.Green;
            else if (Global.work_mode == 4) flowLineBtn.BackColor = Color.Green;
            else if (Global.work_mode == 5) flowLineBackBtn.BackColor = Color.Green;
            else if (Global.work_mode == 6) cleanGlueBtn.BackColor = Color.Green;
            //更新胶量
            glue1NumLabel.Text = Global.glueNum[0].ToString();
            glue2NumLabel.Text = Global.glueNum[1].ToString();
            glue3NumLabel.Text = Global.glueNum[2].ToString();
            glue4NumLabel.Text = Global.glueNum[3].ToString();
            glue5NumLabel.Text = Global.glueNum[4].ToString();
            glue6NumLabel.Text = Global.glueNum[5].ToString();
            glue7NumLabel.Text = Global.glueNum[6].ToString();
            glue8NumLabel.Text = Global.glueNum[7].ToString();
            glue9NumLabel.Text = Global.glueNum[8].ToString();
            glue10NumLabel.Text = Global.glueNum[9].ToString();
            glue11NumLabel.Text = Global.glueNum[10].ToString();
            glue12NumLabel.Text = Global.glueNum[11].ToString();
            glue13NumLabel.Text = Global.glueNum[12].ToString();
        }
        
        private void StartBtn_Click(object sender, EventArgs e)//启停
        {
            if (Global.my_start == 0)
            {
                if (Global.bed_num == 0)
                {
                    MessageBox.Show("计数为零，请从新设置张数！");                    
                    return;
                }
                Global.my_start = 1;
                StartBtn.Text = "停止";
                StartBtn.BackColor = Color.Red;
                myThread = new Thread(countThread);
                myThread.Start();
                SerialServer.send_start(0x01);
                workModeGroupBox.Enabled = false;
                numPanel.Enabled = false;
                bordLenthGroupBox.Enabled = false;
                colNumGroupBox.Enabled = false;
                glueNumGroupBox.Enabled = false;
            }
            else
            {
                Global.my_start = 0;
                StartBtn.Text = "启动";
                myThread.Abort();//终止线程
                StartBtn.BackColor = Color.Green;  
                SerialServer.send_start(0x00);
                workModeGroupBox.Enabled = true;
                numPanel.Enabled = true;
                bordLenthGroupBox.Enabled = true;
                colNumGroupBox.Enabled = true;
                glueNumGroupBox.Enabled = true;
            }
        }

        private void Col_1_Btn_Click(object sender, EventArgs e)//控制第1列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low ;
            if ((lowByte & 0x01) != 0) //失能
            {
                lowByte &= 0xfe;
                Col_1_Btn.BackColor = Color.DarkGray; 
            }
            else //使能
            {
                lowByte |= 0x01;
                Col_1_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState( highByte,lowByte);//发送
        }

        private void Col_2_Btn_Click(object sender, EventArgs e)//控制第2列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x02) != 0) //失能
            {
                lowByte &= 0xfd;
                Col_2_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x02;
                Col_2_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_3_Btn_Click(object sender, EventArgs e)//控制第3列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x04) != 0) //失能
            {
                lowByte &= 0xfb;
                Col_3_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x04;
                Col_3_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_4_Btn_Click(object sender, EventArgs e)//控制第4列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x08) != 0) //失能
            {
                lowByte &= 0xf7;
                Col_4_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x08;
                Col_4_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_5_Btn_Click(object sender, EventArgs e)//控制第5列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x10) != 0) //失能
            {
                lowByte &= 0xef;
                Col_5_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x10;
                Col_5_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_6_Btn_Click(object sender, EventArgs e)//控制第6列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x20) != 0) //失能
            {
                lowByte &= 0xdf;
                Col_6_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x20;
                Col_6_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_7_Btn_Click(object sender, EventArgs e)//控制第7列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x40) != 0) //失能
            {
                lowByte &= 0xbf;
                Col_7_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x40;
                Col_7_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_8_Btn_Click(object sender, EventArgs e)//控制第8列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((lowByte & 0x80) != 0) //失能
            {
                lowByte &= 0x7f;
                Col_8_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                lowByte |= 0x80;
                Col_8_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_9_Btn_Click(object sender, EventArgs e)//控制第9列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((highByte & 0x01) != 0) //失能
            {
                highByte &= 0xfe;
                Col_9_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                highByte |= 0x01;
                Col_9_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_10_Btn_Click(object sender, EventArgs e)//控制第10列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((highByte & 0x02) != 0) //失能
            {
                highByte &= 0xfd;
                Col_10_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                highByte |= 0x02;
                Col_10_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }
        private void Col_11_Btn_Click(object sender, EventArgs e)//控制第11列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((highByte & 0x04) != 0) //失能
            {
                highByte &= 0xfb;
                Col_11_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                highByte |= 0x04;
                Col_11_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_12_Btn_Click(object sender, EventArgs e)//控制第12列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((highByte & 0x08) != 0) //失能
            {
                highByte &= 0xf7;
                Col_12_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                highByte |= 0x08;
                Col_12_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void Col_13_Btn_Click(object sender, EventArgs e)//控制第13列
        {
            byte highByte = Global.col_data_high, lowByte = Global.col_data_low;
            if ((highByte & 0x10) != 0) //失能
            {
                highByte &= 0xef;
                Col_13_Btn.BackColor = Color.DarkGray;
            }
            else //使能
            {
                highByte |= 0x10;
                Col_13_Btn.BackColor = Color.Green;
            }
            SerialServer.send_ColState(highByte, lowByte);//发送
        }

        private void boradLenth0Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(0);//边距6+0/2=6
            boradLenth0Btn.BackColor = Color.Green;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth1Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(1);//边距6+1/2=6.5
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.Green;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth2Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(2);//边距6+2/2=7
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.Green;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth3Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(3);//边距6+3/2=7.5
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.Green;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth4Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(4);//边距6+4/2=8
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.Green;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth5Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(5);//边距6+5/2=8.5
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.Green;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth6Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(6);//边距6+6/2=9
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.Green;
            boradLenth7Btn.BackColor = Color.DarkGray;
        }

        private void boradLenth7Btn_Click(object sender, EventArgs e)
        {
            SerialServer.send_length(7);//边距6+7/2=9.5
            boradLenth0Btn.BackColor = Color.DarkGray;
            boradLenth1Btn.BackColor = Color.DarkGray;
            boradLenth2Btn.BackColor = Color.DarkGray;
            boradLenth3Btn.BackColor = Color.DarkGray;
            boradLenth4Btn.BackColor = Color.DarkGray;
            boradLenth5Btn.BackColor = Color.DarkGray;
            boradLenth6Btn.BackColor = Color.DarkGray;
            boradLenth7Btn.BackColor = Color.Green;
        }

        private void rowAddBtn_Click(object sender, EventArgs e)//增加排
        {
            if (Global.row_num < 30) 
            { 
                Global.row_num+=1;
                 SerialServer.send_RowNum(Global.row_num);
                 rowNumLabel.Text = Global.row_num.ToString();
            } 
           
        }

        private void rowDecBtn_Click(object sender, EventArgs e)//减少排
        {
            if (Global.row_num >0)
            {
                Global.row_num -= 1;
                SerialServer.send_RowNum(Global.row_num);
                rowNumLabel.Text = Global.row_num.ToString();
            } 
        }
        private void bedAddBtn_Click(object sender, EventArgs e)//张数加
        {
            if (Global.bed_num < 1000) 
            {
                Global.bed_num += 1;
                SerialServer.send_BedNum((short)(Global.bed_num)); 
            }
            bedNumLabel.Text = Global.bed_num.ToString();           
        }
        private void bedDecBtn_Click(object sender, EventArgs e)//张数减
        {
            if (Global.bed_num >0)
            {
                Global.bed_num -= 1;
                SerialServer.send_BedNum((short)(Global.bed_num));
            }
            bedNumLabel.Text = Global.bed_num.ToString();
        }


        private void glue1NumAdd_Click(object sender, EventArgs e)//1号胶阀加胶量
        {
            int index = 0;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue1NumLabel.Text = Global.glueNum[index].ToString();
        }


        private void glue1NumDec_Click(object sender, EventArgs e) //1号胶阀减胶量
        {
            int index = 0;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue1NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue2NumAdd_Click(object sender, EventArgs e)//2号胶阀加胶量
        {
            int index = 1;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue2NumLabel.Text = Global.glueNum[index].ToString();
        } 

        private void glue2NumDec_Click(object sender, EventArgs e) //2号胶阀减胶量
        {
            int index = 1;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue2NumLabel.Text = Global.glueNum[index].ToString();
        }



        private void glue3NumAdd_Click(object sender, EventArgs e)//3号胶阀加胶量
        {
            int index = 2;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue3NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue3NumDec_Click(object sender, EventArgs e)//3号胶阀减胶量
        {
            int index = 2;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue3NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue4NumAdd_Click(object sender, EventArgs e)//4号胶阀加胶量
        {
            int index = 3;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue4NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue4NumDec_Click(object sender, EventArgs e)//4号胶阀减胶量
        {
            int index = 3;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue4NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue5NumAdd_Click(object sender, EventArgs e)//5号胶阀加胶量
        {
            int index = 4;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue5NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue5NumDec_Click(object sender, EventArgs e)//5号胶阀减胶量
        {
            int index = 4;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue5NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue6NumAdd_Click(object sender, EventArgs e)//6号胶阀加胶量
        {
            int index = 5;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue6NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue6NumDec_Click(object sender, EventArgs e)//6号胶阀减胶量
        {
            int index = 5;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue6NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue7NumAdd_Click(object sender, EventArgs e)//7号胶阀加胶量
        {
            int index = 6;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue7NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue7NumDec_Click(object sender, EventArgs e)//7号胶阀减胶量
        {
            int index = 6;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue7NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue8NumAdd_Click(object sender, EventArgs e)//8号胶阀加胶量
        {
            int index = 7;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue8NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue8NumDec_Click(object sender, EventArgs e)//8号胶阀减胶量
        {
            int index = 7;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue8NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue9NumAdd_Click(object sender, EventArgs e)//9号胶阀加胶量
        {
            int index = 8;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue9NumLabel.Text = Global.glueNum[index].ToString();
        }
        private void glue9NumDec_Click(object sender, EventArgs e)//9号胶阀减胶量
        {
            int index = 8;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue9NumLabel.Text = Global.glueNum[index].ToString();
        }
        private void glue10NumAdd_Click(object sender, EventArgs e)//10号胶阀加胶量
        {
            int index = 9;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue10NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue10NumDec_Click(object sender, EventArgs e)//10号胶阀减胶量
        {
            int index = 9;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue10NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue11NumAdd_Click(object sender, EventArgs e)//11号胶阀加胶量
        {
            int index = 10;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue11NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue11NumDec_Click(object sender, EventArgs e)//11号胶阀减胶量
        {
            int index = 10;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue11NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue12NumAdd_Click(object sender, EventArgs e)//12号胶阀加胶量
        {
            int index = 11;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue12NumLabel.Text = Global.glueNum[index].ToString();
        }
        private void glue12NumDec_Click(object sender, EventArgs e)//12号胶阀减胶量
        {
            int index = 11;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue12NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue13NumAdd_Click(object sender, EventArgs e)//13号胶阀加胶量
        {
            int index = 12;
            if (Global.glueNum[index] < 10)
            {
                Global.glueNum[index] += 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue13NumLabel.Text = Global.glueNum[index].ToString();
        }

        private void glue13NumDec_Click(object sender, EventArgs e)//13号胶阀减胶量
        {
            int index = 12;
            if (Global.glueNum[index] > 0)
            {
                Global.glueNum[index] -= 1;
                SerialServer.send_glue_num((byte)index, Global.glueNum[index]);
            }
            glue13NumLabel.Text = Global.glueNum[index].ToString();
        }




        private void holeAndGlueAndMagnetBtn_Click(object sender, EventArgs e)//切孔+滴胶+放磁 模式
        {
            SerialServer.send_work_mode(0);
            holeAndGlueAndMagnetBtn.BackColor = Color.Green;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }
        private void glueAndMagnetBtn_Click(object sender, EventArgs e)//滴胶+放磁 模式
        {
            SerialServer.send_work_mode(1);
            holeAndGlueAndMagnetBtn.BackColor =Color.DarkGray; 
            glueAndMagnetBtn.BackColor = Color.Green;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }

        private void onlyGlueBtn_Click(object sender, EventArgs e)//只滴胶模式
        {
            SerialServer.send_work_mode(2);
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.Green;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }
        private void onlyMagnetBtn_Click(object sender, EventArgs e)//只放磁 模式
        {
            SerialServer.send_work_mode(3);
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.Green;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }

        private void flowLineBtn_Click(object sender, EventArgs e)//流水线模式
        {
            SerialServer.send_work_mode(4);
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.Green;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }

        private void flowLineBackBtn_Click(object sender, EventArgs e)//回流模式
        {
            SerialServer.send_work_mode(5);
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.Green;
            cleanGlueBtn.BackColor = Color.DarkGray;
        }

        private void cleanGlueBtn_Click(object sender, EventArgs e)//清洗 模式
        {
            SerialServer.send_work_mode(6);
            holeAndGlueAndMagnetBtn.BackColor = Color.DarkGray;
            glueAndMagnetBtn.BackColor = Color.DarkGray;
            onlyGlueBtn.BackColor = Color.DarkGray;
            onlyMagnetBtn.BackColor = Color.DarkGray;
            flowLineBtn.BackColor = Color.DarkGray;
            flowLineBackBtn.BackColor = Color.DarkGray;
            cleanGlueBtn.BackColor = Color.Green;
        }

       
    }
}
