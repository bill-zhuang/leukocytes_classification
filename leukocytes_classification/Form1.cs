using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace leukocytes_classification
{
    public partial class Form1 : Form
    {
        private int signNum = 0;
        string[] dataRow = new string[10];

        List<Bitmap> originBmp = new List<Bitmap>();
        List<Bitmap> dstTranBmp = new List<Bitmap>();
        List<Bitmap> lsBmp = new List<Bitmap>();

        public Form1()
        {
            InitializeComponent();
        }

        private void btnDstTran_Click(object sender, EventArgs e)
        {
            if (signNum == 5)
            {
                dgvFeature.Rows.Clear();
            }

            lsBmp.Clear();
            dstTranBmp.Clear();
            originBmp.Clear();
            OpenFileDialog ofd1 = new OpenFileDialog();
            ofd1.Multiselect = true;
            ofd1.Filter = "PNG files (*.png)|*.png|" +
                "Image files (*.jpg,*.png,*.tif,*.bmp,*.gif)|*.jpg;*.png;*.tif;*.bmp;*.gif|JPG fil" +
                "es (*.jpg)|*.jpg|TIF files (*.tif)|*.tif|BMP files (*.bm" +
                "p)|*.bmp|GIF files (*.gif)|*.gif";
            //ofd.RestoreDirectory = true;
            ofd1.Title = "Open image";
            ofd1.FileName = "";

            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in ofd1.FileNames)
                {
                    //后缀名
                    string fExten = Path.GetExtension(filename).ToLower();

                    if (fExten == ".png")//.png
                    {
                        if (signNum == 0)
                        {
                            dataRow[0] = "-1";
                        }
                        else if (signNum <= 4)
                        {
                            dataRow[0] = "+" + signNum.ToString();
                        }
                        else
                        {
                            dataRow[0] = "+5";
                        }

                        Bitmap image = (Bitmap)Bitmap.FromFile(filename);
                        //originBmp.Add(image);

                        string[] data = LeukocytesFeature.FeatureExtraction(image, filename);

                        for (int i = 1; i < data.Length; i++)
                        {
                            dataRow[i] = data[i];
                        }
                        dgvFeature.Rows.Add(dataRow);
                    }
                }
            }

            ofd1.Multiselect = false;
            signNum++;

        }

        private void btnSaveData_Click(object sender, EventArgs e)
        {
            if (dgvFeature.Rows.Count > 0)
            {
                string filename;
                SaveFileDialog saveTxt = new SaveFileDialog();

                saveTxt.Filter = "数据文件 (*.txt)|*.txt";

                if (saveTxt.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        filename = saveTxt.FileName;

                        FileStream fs = new FileStream(filename, FileMode.CreateNew);

                        for (int j = 0; j < dgvFeature.Rows.Count - 1; j++)
                        {
                            string str = dgvFeature[0, j].Value + " ";

                            for (int i = 1; i < dgvFeature.Columns.Count; i++)
                            {
                                if (dgvFeature[i, j].Value != null)
                                {
                                    str += i.ToString() + ":" + dgvFeature[i, j].Value + " ";
                                }

                                if (i == dgvFeature.Columns.Count - 1)
                                {
                                    str += "\r\n";
                                }
                            }

                            byte[] rByte = System.Text.Encoding.Default.GetBytes(str.ToCharArray());
                            fs.Write(rByte, 0, rByte.Length);
                        }

                        fs.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        saveTxt.Dispose();
                    }
                }
            }
            else
            { 
                
            }
        }

    }
}
