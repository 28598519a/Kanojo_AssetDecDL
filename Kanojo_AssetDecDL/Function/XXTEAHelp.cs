using System;
using System.Collections.Generic;
using System.Text;

public class XXTEAHelp
{
    /// <summary>
    /// 確定加密的密鑰,加密字節流
    /// </summary>
    /// <param name="data">待加密的字節流</param>
    /// <param name="data_len">待加密的字節長度</param>
    /// <param name="key">加密的密鑰字節流</param>
    /// <param name="key_len">密鑰的長度</param>
    /// <param name="ret_length">加密後返回的字節流長度</param>
    /// <returns>返回加密後的字節流</returns>
    public byte[] xxtea_encrypt(byte[] data, uint data_len, byte[] key, uint key_len, out uint ret_length)
	{
		byte[] result;
		
		ret_length = 0;
		/*密鑰長度小於16字節的用'\0'填充*/
		if (key_len < 16) {
			byte[] key2 = fix_key_length(key, key_len);
			result = do_xxtea_encrypt(data, data_len, key2, out ret_length);
			//free(key2);
		}
		else
		{
			result = do_xxtea_encrypt(data, data_len, key, out ret_length);
		}
		
		return result;
	}
	
	//將不滿16位的密鑰後面用'\0'填充
	private  byte[] fix_key_length(byte[] key, uint key_len)
	{
		byte[] tmp = new byte[16];
		Array.Copy(key, tmp, key_len);
		for(uint i = key_len; i < tmp.Length; i++)
		{
			tmp[i] = 0;		//剩餘字節用'\0'填充
		}
		return tmp;
	}
    /// <summary>
    /// 確定加密使用的v.k 在對其轉換為uint32流後加密,在轉換為字節流
    /// </summary>
    /// <param name="data">待加密的字節流</param>
    /// <param name="len">加密的字節長度</param>
    /// <param name="key">加密的密鑰字節流</param>
    /// <param name="ret_len">返回加密後的字節流長度</param>
    /// <returns>返回加密後的字節流</returns>
    private byte[] do_xxtea_encrypt(byte[] data, uint len, byte[] key, out uint ret_len) {
		byte[] result;
		uint[] v, k;
		//xxtea_long *v, *k, v_len, k_len;
		uint v_len, k_len;
		v = xxtea_to_long_array(data, len, 1, out v_len);
		k = xxtea_to_long_array(key, 16, 0, out k_len);
		xxtea_long_encrypt(ref v, v_len, ref k);
		result = xxtea_to_byte_array(v, v_len, 0, out ret_len);		
		return result;
	}
    /// <summary>
    /// 將字節流轉換為uint流
    /// </summary>
    /// <param name="data">待轉換的字節流</param>
    /// <param name="len">轉換字節流的長度</param>
    /// <param name="include_length">開關,等於1則分配(len % 4) ? len / 4 + 1 : len / 4 + 1 + 1個元素空間,
    /// 等於0則分配 (len % 4) ? len / 4 : len / 4 + 1個元素空間</param>
    /// <param name="ret_len">返回轉換後的數組長度</param>
    /// <returns>返回轉換後的uint數組流</returns>
    private uint[] xxtea_to_long_array(byte[] data, uint len, int include_length, out uint ret_len) {
		//xxtea_long i, n, *result;
		uint i, n;
		uint[] result;

		n = len >> 2;	
	
		n = (((len & 3) == 0) ? n : n + 1); 	//確保最後一個是否為0
		if (include_length == 1) {
			//申請n + 1個元素,最後一個元素保存原字節流的長度
			result = new uint[(n + 1) << 2];
			result[n] = len;
			ret_len = n + 1;
		} else {
			result = new uint[n << 2];
			ret_len = n;
		}
		for (i = 0; i < len; i++) {
           

			result[i >> 2] |= (uint)data[i]<< (int)((i & 3) << 3);
		}
		
		return result;
	}
    /*#define XXTEA_MX (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z)
	  #define XXTEA_DELTA 0x9e3779b9*/
    /// <summary>
    /// XXTEA算法加密uint流
    /// </summary>
    /// <param name="v">返回算法加密後的uint流</param>
    /// <param name="len">加密前的uint流長度</param>
    /// <param name="k">參與運算的k密鑰</param>
    private void xxtea_long_encrypt(ref uint[] v, uint len, ref uint[] k)
	{
		uint n = len - 1;
		uint z = v[n], y = v[0], p, q = 6 + 52 / (n + 1), sum = 0, e;
		if (n < 1) {
			return;
		}
		while (0 < q--) {
			sum += 0x9e3779b9;
			e = sum >> 2 & 3;
			/*當前數組元素與下個數組元素參與運算*/
			for (p = 0; p < n; p++) {
				y = v[p + 1];
				z = v[p] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
			}
			/*第一個數組元素與最後一個數組元素運算*/
			y = v[0];
			z = v[n] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
		}
	}
    /*#define XXTEA_MX (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z)
	  #define XXTEA_DELTA 0x9e3779b9*/
    /// <summary>
    /// XXTEA算法解密uint流
    /// </summary>
    /// <param name="v">返回算法解密後的uint流</param>
    /// <param name="len">密前的uint流長度</param>
    /// <param name="k">參與運算的密鑰</param>
    private void xxtea_long_decrypt(ref uint[] v, uint len, ref uint[] k)
	{
		uint n = len - 1;
		uint z = v[n], y = v[0], p, q = 6 + 52 / (n + 1), sum = q * 0x9e3779b9, e;
		if (n < 1) {
			return;
		}
		while (sum != 0) {
			e = sum >> 2 & 3;
			for (p = n; p > 0; p--) {
				z = v[p - 1];
				y = v[p] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
			}
			z = v[n];
			y = v[0] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z);
			sum -= 0x9e3779b9;
		}
	}
    /// <summary>
    /// uint流轉換為字節流
    /// </summary>
    /// <param name="data">待轉換的uint流</param>
    /// <param name="len">待轉換的uint流的長度</param>
    /// <param name="include_length">包含長度開關</param>
    /// <param name="ret_len">返回轉換後的字節流長度</param>
    /// <returns>回轉換後的字節流</returns>
    private byte[] xxtea_to_byte_array(uint[] data, uint len, int include_length, out uint ret_len) {
		uint i, n, m;
		byte[] result;
        ret_len = 0;
		n = len << 2;
		if (include_length == 1) {
			m = data[len - 1];
			if ((m < n - 7) || (m > n - 4)) return null;
			n = m;
		}
        //此處修改了源碼
		result = new byte[n];
		for (i = 0; i < n; i++) {
			result[i] = (byte)((data[i >> 2] >> (int)((i & 3) << 3)) & 0xff);
		}
		//result[n] = 0;
		ret_len = n;
		
		return result;
	}
	/*******************************decrypt****************************************/
	/// <summary>
    /// 解密字節流
    /// </summary>
    /// <param name="data">待解密的字節流</param>
    /// <param name="data_len">解密的字節長度</param>
    /// <param name="key">解密需要的密鑰</param>
    /// <param name="key_len">密鑰的長度</param>
    /// <param name="ret_length">返回解密後的長度</param>
    /// <returns></returns>
    public byte[] xxtea_decrypt(byte[] data, uint data_len, byte[] key, uint key_len, out uint ret_length)
	{
		byte[] result;
		
		ret_length = 0;
		
		if (key_len < 16) {
			byte[] key2 = fix_key_length(key, key_len);
			result = do_xxtea_decrypt(data, data_len, key2, out ret_length);
			
		}
		else
		{
			result = do_xxtea_decrypt(data, data_len, key, out ret_length);
		}
		
		return result;
	}
	/// <summary>
    /// XXTEA解密字節流
    /// </summary>
    /// <param name="data">待解密的字節流</param>
    /// <param name="len">解密的字節流長度</param>
    /// <param name="key">解密的密鑰</param>
    /// <param name="ret_len">返回解密後的長度</param>
    /// <returns></returns>
    private  byte[] do_xxtea_decrypt(byte[] data, uint len, byte[] key, out uint ret_len) {
		byte[] result;
		uint[] v, k;
		uint v_len, k_len;
		
		v = xxtea_to_long_array(data, len, 0, out v_len);	//確定v的值
		k = xxtea_to_long_array(key, 16, 0, out k_len);	//確定k的值
		xxtea_long_decrypt(ref v, v_len, ref k);
		result = xxtea_to_byte_array(v, v_len, 1, out ret_len);
		return result;
	}
	
}