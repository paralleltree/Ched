using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched
{
    public class Base64Utility
    {

        //64進数で使う文字
        private readonly static List<char> CHAR_LIST = new List<char>(){
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
    'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
    'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D',
    'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
    'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
    'Y', 'Z', '+', '/'
  };

        //=================================================================================
        //64進数に変換
        //=================================================================================

        /// <summary>
        /// 2進数を64進数に変換する
        /// </summary>
        public static string BinaryToBase64(string binaryNum)
        {

            //6桁(64 = 2^6)毎に区切れるように、足りない分だけ0を追加する
            int digit = (1 + ((int)(binaryNum.Length - 1) / 6)) * 6;
            binaryNum = binaryNum.PadLeft(digit, '0');

            //6桁毎に変換し、入力値を64進数で表した文字列を作成
            string base64 = "";

            for (int i = 0; i < binaryNum.Length; i += 6)
            {
                int no = Convert.ToInt32(binaryNum.Substring(i, 6), 2);
                base64 += CHAR_LIST[no];
            }

            return base64;
        }

        /// <summary>
        /// 8進数を64進数に変換する
        /// </summary>
        public static string OctalToBase64(string octalNum)
        {
            //8進数→10進数→2進数→64進数
            return BinaryToBase64(Convert.ToString(Convert.ToInt32(octalNum, 8), 2));
        }

        /// <summary>
        /// 10進数を64進数に変換する
        /// </summary>
        public static string DecimalToBase64(int decimalNum)
        {
            //10進数→2進数→64進数
            return BinaryToBase64(Convert.ToString(decimalNum, 2));
        }

        /// <summary>
        /// 16進数を64進数に変換する
        /// </summary>
        public static string HexadecimalToBase64(string hexadecimalNum)
        {
            //16進数→10進数→2進数→64進数
            return BinaryToBase64(Convert.ToString(Convert.ToInt32(hexadecimalNum, 16), 2));
        }

        //=================================================================================
        //64進数を変換
        //=================================================================================

        /// <summary>
        /// 64進数を2進数に変換する
        /// </summary>
        public static string Base64ToBinary(string base64)
        {
            //一文字ずつ、6桁(64 = 2^6)の2進数に直していく
            string binaryNum = "";

            for (int i = 0; i < base64.Length; i++)
            {
                for (int listNo = 0; listNo < CHAR_LIST.Count; listNo++)
                {
                    if (base64[i] == CHAR_LIST[listNo])
                    {
                        binaryNum += Convert.ToString(listNo, 2).PadLeft(6, '0');
                        break;
                    }
                }
            }

            return binaryNum;
        }

        /// <summary>
        /// 64進数を8進数に変換する
        /// </summary>
        public static string Base64ToOctal(string base64)
        {
            //64進数→2進数→10進数→8進数
            return Convert.ToString(Convert.ToInt64(Base64ToBinary(base64), 2), 8);
        }

        /// <summary>
        /// 64進数を10進数に変換する
        /// </summary>
        public static int Base64ToDecimal(string base64)
        {
            //64進数→2進数→10進数
            return Convert.ToInt32(Base64ToBinary(base64), 2);
        }

        /// <summary>
        /// 64進数を16進数に変換する
        /// </summary>
        public static string Base64ToHexadecimal(string base64)
        {
            //64進数→2進数→10進数→16進数
            return Convert.ToString(Convert.ToInt64(Base64ToBinary(base64), 2), 16);
        }

    }
}
