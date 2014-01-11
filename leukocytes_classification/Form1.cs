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
            ofd1.Filter = "PNG files (*.png)|*.png";
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
                        dataRow[9] = Path.GetFileNameWithoutExtension(filename);
                        dgvFeature.Rows.Add(dataRow);
                    }
                }
            }

            ofd1.Multiselect = false;
            signNum++;

            MessageBox.Show("特征提取成功，部分白细胞已分类（单核细胞、淋巴细胞、嗜碱性粒细胞及嗜酸性粒细胞和中性粒细胞大类）,分类结果的图像放在D:/leukocytes目录下");
        }

        private void btnSaveData_Click(object sender, EventArgs e)
        {
            if (dgvFeature.Rows.Count > 0)
            {
                string filename;
                SaveFileDialog saveTxt = new SaveFileDialog();

                //saveTxt.Filter = "数据文件 (*.txt)|*.txt";

                //if (saveTxt.ShowDialog() == DialogResult.OK)
                //{
                try
                {
                    filename = "TEST.txt";//saveTxt.FileName;

                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }

                    FileStream fs = new FileStream(filename, FileMode.CreateNew);

                    for (int j = 0; j < dgvFeature.Rows.Count - 1; j++)
                    {
                        if (dgvFeature[dgvFeature.Columns.Count - 3, j].Value != null)
                        {
                            string str = "+5 ";// dgvFeature[0, j].Value + " ";

                            for (int i = 4; i < dgvFeature.Columns.Count - 1; i++)
                            {
                                if (dgvFeature[i, j].Value != null)
                                {
                                    str += (i - 3).ToString() + ":" + dgvFeature[i, j].Value + " ";
                                }

                                if (i == dgvFeature.Columns.Count - 2)
                                {
                                    str += "\r\n";
                                }
                            }

                            byte[] rByte = System.Text.Encoding.Default.GetBytes(str.ToCharArray());
                            fs.Write(rByte, 0, rByte.Length);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    fs.Dispose();

                    //save leukocytes image name to picname.txt under bin/debug folder.
                    filename = "picname.txt";
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    fs = new FileStream(filename, FileMode.CreateNew);

                    for (int j = 0; j < dgvFeature.Rows.Count - 1; j++)
                    {
                        if (dgvFeature[dgvFeature.Columns.Count - 2, j].Value != null)
                        {
                            string str = dgvFeature[dgvFeature.Columns.Count - 1, j].Value.ToString();
                            str = str.Trim() + "\r\n";

                            byte[] rByte = System.Text.Encoding.Default.GetBytes(str.ToCharArray());
                            fs.Write(rByte, 0, rByte.Length);
                        }
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
                //}
                MessageBox.Show("已保存数据（仅保存嗜酸性粒细胞和中性粒细胞数据）)，在bin/debug目录下");
            }
            else
            {
                MessageBox.Show("无数据");
            }

        }

        private void btnSvm_Click(object sender, EventArgs e)
        {
            LeukocytesFeature.SVMClassification();
            MessageBox.Show("svm分类成功,分类结果的图像放在D:/leukocytes目录下");
        }

    }
}
