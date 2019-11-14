using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;//线程
using System.IO.Ports; //串口通讯必需
using System.IO;//文件
namespace HeYeAIO
{
    class SerialServer
    {
        public static SerialPort R485Port = new SerialPort();//串口
        public static byte[] Device_Id = new byte[3] { 0x51, 0x52, 0x53 };//设备ID
        public static void SerialPort_init()
        {      
            //查询串口
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
           
            //初始化SerialPort对象
            R485Port.PortName = ports[0];//串口名称
            R485Port.BaudRate = 115200;// 波特率115200;                
            R485Port.RtsEnable = true;//根据实际情况吧。
            R485Port.Parity = System.IO.Ports.Parity.None;//.Even;//E=偶校验
            R485Port.DataBits = 8;//数据位  
            R485Port.StopBits = System.IO.Ports.StopBits.One;//One//Two;//停止位:1
            R485Port.RtsEnable = true;
            //添加事件注册
            R485Port.DataReceived += receive_message;
            R485Port.Open();
            R485Port.DiscardOutBuffer();
            R485Port.DiscardInBuffer();
        }
        /// <summary>
        /// //串口接收:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void receive_message(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data = new byte[8];
            if (!R485Port.IsOpen) return;//关闭
            try
            {
                Thread.Sleep(50);
                int n = R485Port.BytesToRead;
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
                R485Port.Read(buf, 0, n);//读取缓冲数据

                MainForm.receiveMessage += Encoding.ASCII.GetString(buf);//转换ASCII               
            }
            catch
            {
                MainForm.receiveMessage += "接收异常！\r\n";
            }
        }
        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>成功与否</returns>
        public static bool send_cmd(byte Device_Id,byte[] data)
        {
            int i = 0, n = 0;
            byte temp = 0x00;
            byte[] uart_data = new byte[8];
            uart_data[0] = Device_Id;
            uart_data[1] = 0xaa;
            uart_data[2] = data[0];   //cmd 1:start/stop, 2:col_data, 3:排数 4:length（海绵切孔前边距）,5:张数, 6:工作模式, 7：胶量设置 8：信号灯
            uart_data[3] = data[1];   //data
            uart_data[4] = data[2];   //data
            uart_data[5] = data[3];   //data
            uart_data[6] = data[4];   //data
            uart_data[7] = 0x00;      //sum check
            R485Port.DataReceived -= receive_message;//屏蔽接收

            for (i = 2; i < 7; i++) { uart_data[7] += uart_data[i]; }
            R485Port.Write(uart_data, 0, 8);

            System.Threading.Thread.Sleep(30);//发送后延时,再接收

            n = R485Port.BytesToRead;
            byte[] recv_ha = new byte[n];
            R485Port.Read(recv_ha, 0, n);
            R485Port.DataReceived += receive_message;//使能接收
            if (n >= 8)
            {
                for (i = 2; i < 7; i++) { temp += recv_ha[i]; }   //sum check
                if (recv_ha[0] == 0xaa && recv_ha[1] == Device_Id
                    && recv_ha[2] == data[0] && recv_ha[7] == temp)
                {
                    data[1] = recv_ha[3];
                    data[2] = recv_ha[4];
                    data[3] = recv_ha[5];
                    data[4] = recv_ha[6];
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// //发送启动停止命令
        /// </summary>
        public static void send_start(byte startState)
        {
            byte[] data = new byte[5];

            data[0] = 0x01;   //cmd start/stop           
            data[1] = startState; //状态
            // while (!send_cmd(Device_Id[0], data)) { System.Threading.Thread.Sleep(50); }  
            //while (!send_cmd(Device_Id[1], data)) { System.Threading.Thread.Sleep(50); } 
            send_cmd(Device_Id[0], data);
            Thread.Sleep(100);
            send_cmd(Device_Id[1], data);
        }

        /// <summary>
        ///  //发送列切刀使能位
        /// </summary>
        /// <param name="highByte"></param>
        /// <param name="lowByte"></param>
        public static void send_ColState(byte highByte, byte lowByte)
        {
            byte[] data = new byte[5];
            if (Global.my_start == 1) return;//运行时不操作

            data[0] = 0x02;    //cmd colState
            data[1] =  highByte;  //data
            data[2] =  lowByte;  //data

            while (!send_cmd(Device_Id[0], data)) { System.Threading.Thread.Sleep(50); }
            while (!send_cmd(Device_Id[1], data)) { System.Threading.Thread.Sleep(50); } 
            //切刀使能位保存到文件中
            Global.savebuf[0] = Global.col_data_high = data[1];
            Global.savebuf[1] = Global.col_data_low = data[2];
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存列状态
        }
        //发送切孔排数
        public static void send_RowNum(byte num)
        {
            byte[] data = new byte[5];
            if (Global.my_start == 1) return;//运行时不操作

            data[0] = 0x03;    //cmd RowNum
            data[1] = num;  //data

            while (!send_cmd(Device_Id[0], data) ) { System.Threading.Thread.Sleep(50); }
            Global.savebuf[2] = Global.row_num = data[1]; 
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存前边距
        }
        //发送海绵切孔前边距
        public static void send_length(byte length)
        {
            byte[] data = new byte[5];

            if (Global.my_start == 1) return;//运行时不操作

            data[0] = 0x04;    //cmd bord_length
            data[1] = length;   //length:(6+n/2)(cm)
            while (!send_cmd(Device_Id[0], data) ) { System.Threading.Thread.Sleep(50); }
            Global.savebuf[3] = Global.bord_length = data[1];
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存前边距
        }
        //发送海绵张数
        public static void send_BedNum(short num)
        {
            byte[] data = new byte[5];
           
            data[0] = 0x05;    //cmd BedNum
            data[1] = (byte)(num >> 8);//高位
            data[2] = (byte)(num & 0xff);//低位
            while (!send_cmd(Device_Id[0], data) ) { System.Threading.Thread.Sleep(50); }

            Global.bed_num = (short)(data[1] << 8 | data[2]);
            Global.savebuf[4] = data[1];
            Global.savebuf[5] = data[2];
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存张数             
        }

        //发送工作模式
        public static void send_work_mode(byte workMode)
        {
            byte[] data = new byte[5];

            if (Global.my_start == 1) return;//运行时不操作

            data[0] = 0x06;    //cmd bord_length
            data[1] = workMode;   //mode


            while (!send_cmd(Device_Id[0], data) ) { System.Threading.Thread.Sleep(50); }
            Global.savebuf[6] = Global.work_mode = data[1];
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存前边距
        }

        /// <summary>
        /// //发送出胶量
        /// </summary>
        /// <param name="glueIndex">电磁阀编号0-12</param>
        /// <param name="gluenum">胶量0-10</param>
        public static void send_glue_num(byte glueIndex, byte gluenum)
        {
            byte[] data = new byte[5];
            if (Global.my_start == 1) return;//运行时不操作

            data[0] = 0x07;    //cmd num
            data[1] = glueIndex;   //电磁阀编号
            data[2] = gluenum;   //胶量

            while (!send_cmd(Device_Id[0], data) ) { System.Threading.Thread.Sleep(50); }
            Global.savebuf[glueIndex + 10] = gluenum;
            FileOperation.WriteByteFile("config.txt", Global.savebuf);//保存张数             

        }



        //public static void send_safe_light(byte status)//安全信号灯
        //{
        //    byte[] data = new byte[5];
        //    if (Global.my_start == 1) return;//运行时不操作

        //    data[0] = 0x08;    //cmd num
        //    data[1] = status; //status 1:绿灯 2：红灯  3：红灯+蜂鸣
        //    //while (!send_cmd(Device_Id[1], data) ) { System.Threading.Thread.Sleep(50); }
        //}
        //public static void send_read_weight()//发送读取称的命令
        //{
        //    byte[] data = new byte[5];
        //    if (Global.my_start == 1) return;//运行时不操作

        //    data[0] = 0x09;    //cmd num
        //    //while (!send_cmd(Device_Id[1], data) ) { System.Threading.Thread.Sleep(50); }
        //}

    }
}
