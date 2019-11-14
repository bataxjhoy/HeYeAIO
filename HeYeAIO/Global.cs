using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeYeAIO
{
    public static class Global
    {
        public static byte my_start = 0;//启停标志
        public static byte col_data_low = 0xff, col_data_high = 0xff;//列设置参数
        public static byte row_num = 0;//切孔排数
        public static byte bord_length = 0;//前边距
        public static int bed_num = 0;//张数
        public static byte work_mode = 0;//工作模式
        public static byte[] glueNum = new byte[13];//0-12电磁阀的胶量

        //保存的数据：0：col_data_high  1：col_data_low  2:row_num  3:bord_length  4-5：张数 6:工作模式   10-22：0-12电磁阀的胶量
        public static byte[] savebuf = new byte[23];
       
    }
}
