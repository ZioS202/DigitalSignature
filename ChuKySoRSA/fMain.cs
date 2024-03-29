﻿using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using Waher.Security.EllipticCurves;
using Waher.Security.SHA3;
namespace ChuKySo
{
    public partial class fMain : Form
    {
        public fMain()
        {
            InitializeComponent();
        }
   
        //event mở file muốn ký và chọn thư mục
        private void btChonFileKy_Click(object sender, EventArgs e)
        {
            OpenFileDialog openLinkGui = new OpenFileDialog();
            if (openLinkGui.ShowDialog() == DialogResult.OK)
                textDuongDanGui.Text = openLinkGui.FileName;
            btTaoChuKy.Enabled = true; //chọn xong rồi thì hiện nút Tạo chư ký lên 
        }
        //tương tự nhưng là với file muốn kiểm tra
        private void btChonFileKiemTra_Click(object sender, EventArgs e)
        {
            OpenFileDialog openLinkNhan = new OpenFileDialog();
            if (openLinkNhan.ShowDialog() == DialogResult.OK)
                textDuongDanNhan.Text = openLinkNhan.FileName;
            btKiemTra.Enabled = true;// hiện nút kiểm tra
        }
        //event tạo chữ ký
        Aes myAes;
        private void btTaoChuKy_Click(object sender, EventArgs e)
        {
            // kiểm tra tồn tại path không
            if (!File.Exists(textDuongDanGui.Text))
            {
                MessageBox.Show("Bạn chưa chọn file thực hiện ký!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (File.Exists(textDuongDanGui.Text))
            {
                //Lấy giá trị string của file
                StreamReader fileReader = new StreamReader($"{textDuongDanGui.Text}");
                string text = fileReader.ReadToEnd();
                fileReader.Close();
                fileReader.Dispose();

				//Tạo key và IV
				myAes = Aes.Create();
				myAes.KeySize = 256;
				myAes.BlockSize = 128;
				myAes.Mode = CipherMode.CBC;
				myAes.Padding = PaddingMode.PKCS7;
				myAes.GenerateIV();
				myAes.GenerateKey();

				//Mã hoá AES 
                byte[] encrypted = EncryptStringToBytes_Aes(text, myAes.Key, myAes.IV);

				

				//byte[] byteMaHoa = Encoding.ASCII.GetBytes(text);

                Edwards448 myEd448 = new Edwards448();
                byte[] sign = myEd448.Sign(encrypted);
                textHienThiPublicKey.Text = Convert.ToBase64String(myEd448.PublicKey);
                string tepKyGui = Convert.ToBase64String(sign);
                textTepKyGui.Text = tepKyGui;
                MessageBox.Show("Thực hiện ký thành công !", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btTaoChuKy.Enabled = false;// ẩn nút Tạo chữ ký 

                //Xuất file .txt của file encrypted
                string convert = Convert.ToBase64String(encrypted);
				System.IO.File.WriteAllText(@"D:\TextGui_Encrypted.txt", convert);
				System.IO.File.WriteAllText(@"D:\TextGui_Original.txt", text);
			}
        }

		private void btKiemTra_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textDuongDanNhan.Text))
            {
                MessageBox.Show("Bạn chưa chọn Tài liệu kiểm tra chữ ký", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (File.Exists(textDuongDanNhan.Text))
            {
				StreamReader fsFileDauVao = new StreamReader($"{textDuongDanNhan.Text}");
				string text = fsFileDauVao.ReadToEnd();
				fsFileDauVao.Close();
				fsFileDauVao.Dispose();
                //Convert Stream to bytes
                try
                {
					//byte[] byteEncrypted = Convert.FromBase64String(text);

					//string roundtrip = DecryptStringFromBytes_Aes(byteEncrypted, myAes.Key, myAes.IV);

					//byte[] bytegiaima = Encoding.ASCII.GetBytes(roundtrip);
					byte[] dataToVerify = Convert.FromBase64String(text);
					string originalData = DecryptStringFromBytes_Aes(dataToVerify, myAes.Key, myAes.IV);
						System.IO.File.WriteAllText(@"D:\TextNhan.txt", originalData);

                    Edwards448 giaiMa = new Edwards448();
                    var result = giaiMa.Verify(dataToVerify, Convert.FromBase64String(textHienThiPublicKey.Text), Convert.FromBase64String(textTepKyGui.Text));

                    if (result)
                    {
                        MessageBox.Show("Tài liệu gửi đến không bị chỉnh sửa gì", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btKiemTra.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Tài liệu gửi đến đã bị thay đổi", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btKiemTra.Enabled = false;
                    }
                }
                catch (Exception loi)
                {
                    //MessageBox.Show("Tài liệu gửi đến đã bị thay đổi", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //btKiemTra.Enabled = false;
                }
				
			}
        }
        private void fMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Bạn muốn thoát khỏi ứng dụng ???", "THÔNG BÁO", MessageBoxButtons.OKCancel) != System.Windows.Forms.DialogResult.OK)
                e.Cancel = true;
        }
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
				aesAlg.KeySize = 256;
				aesAlg.BlockSize = 128;
				aesAlg.Mode = CipherMode.CBC;
				aesAlg.Padding = PaddingMode.PKCS7;
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
				aesAlg.KeySize = 256;
				aesAlg.BlockSize = 128;
				aesAlg.Mode = CipherMode.CBC;
				aesAlg.Padding = PaddingMode.PKCS7;
				aesAlg.Key = Key;
				aesAlg.IV = IV;

				// Create a decryptor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
        
    }
}
