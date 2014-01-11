using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using SVM;

namespace leukocytes_classification
{
    public static class LeukocytesFeature
    {
        static string[] data_row = new string[9];
        static float nucleus_area_static = 0;
        static Dictionary<string, string> cell_path = new Dictionary<string, string>();

        private static void init()
        {
            Array.Clear(data_row, 0, data_row.Length);

            nucleus_area_static = 0;

            cell_path["monocytes"] = @"D:\leukocytes\monocytes\";
            cell_path["lymphocytes"] = @"D:\leukocytes\lymphocytes\";
            cell_path["basophils"] = @"D:\leukocytes\basophils\";
            cell_path["eosinophils_and_neutrophils"] = @"D:\leukocytes\eosinophils_and_neutrophils\";
        }

        public static string[] FeatureExtraction(Bitmap gray_bmp, string file_path)
        {
            DistanceTransform(gray_bmp, file_path);

            string cell_type = GetCellTypeByFeatures();
            if (cell_type == "eosinophils_and_neutrophils")
            {
                string color_bmp_path = Path.ChangeExtension(file_path, "jpg");
                Bitmap color_bmp = (Bitmap)Bitmap.FromFile(color_bmp_path);

                int[] nucleus_bytes = new int[gray_bmp.Height * gray_bmp.Width];
                ImageInfo.ImageDataInfo gray_bmp_info = ImageInfo.GetImageBytes(gray_bmp, gray_bmp.PixelFormat);
                ImageInfo.ImageDataInfo color_bmp_info = ImageInfo.GetImageBytes(color_bmp, gray_bmp.PixelFormat);

                double temp = 0;
                for (int j = 1; j < gray_bmp_info.ImageHeight - 1; j++)
                {
                    for (int i = 1; i < gray_bmp_info.ImageWidth - 1; i++)
                    {
                        byte pixel = gray_bmp_info.ImageBytes[j * gray_bmp_info.RowSizeBytes + i];

                        if (pixel == 255)
                        {
                            temp = color_bmp_info.ImageBytes[j * color_bmp_info.RowSizeBytes + i * 3 + 2] * 0.299
                                        + color_bmp_info.ImageBytes[j * color_bmp_info.RowSizeBytes + i * 3 + 1] * 0.587
                                        + color_bmp_info.ImageBytes[j * color_bmp_info.RowSizeBytes + i * 3] * 0.114;

                            nucleus_bytes[j * color_bmp_info.ImageWidth + i] = (byte)temp;
                        }
                        else
                        {
                            nucleus_bytes[j * color_bmp_info.ImageWidth + i] = 256;
                        }
                    }
                }

                Glcm(16, gray_bmp.Width, gray_bmp.Height, nucleus_bytes, nucleus_area_static);

                MoveFile(cell_path[cell_type], file_path);
            }
            else
            {
                MoveFile(cell_path[cell_type], file_path);
            }

            return data_row;
        }

        //update next version by using approximate distance transform, speed is pretty fast.
        private static void DistanceTransform(Bitmap bmp, string file_path)
        {
            init();

            List<Point> nucleus_inner_points_list = new List<Point>();
            List<Point> nucleus_border_points_list = new List<Point>();
            float nucleus_area = 0;
            float cytoplasm_area = 0;
            float nucleus_cytoplasm_ratio = 0;
            float area_perimeter_ratio = 0;
            float nucleus_perimeter = 0;
            float nucleus_roundness = 0;

            ImageInfo.ImageDataInfo bmpInfo = ImageInfo.GetImageBytes(bmp, bmp.PixelFormat);

            for (int j = 1; j < bmpInfo.ImageHeight - 1; j++)
            {
                for (int i = 1; i < bmpInfo.ImageWidth - 1; i++)
                {
                    byte bndPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + i];

                    if (bndPixel == 255)
                    {
                        byte upPixel = bmpInfo.ImageBytes[(j - 1) * bmpInfo.RowSizeBytes + i];
                        byte downPixel = bmpInfo.ImageBytes[(j + 1) * bmpInfo.RowSizeBytes + i];
                        byte leftPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + (i - 1)];
                        byte rightPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + (i + 1)];

                        if (upPixel == 255 && downPixel == 255 && leftPixel == 255 && rightPixel == 255)
                        {
                            nucleus_inner_points_list.Add(new Point(i, j));
                        }
                        else
                        {
                            nucleus_border_points_list.Add(new Point(i, j));
                        }

                        nucleus_area++;
                    }
                    else if (bndPixel == 128)
                    {
                        cytoplasm_area++;
                    }
                }
            }

            nucleus_perimeter = nucleus_border_points_list.Count();
            nucleus_cytoplasm_ratio = nucleus_area / cytoplasm_area;
            area_perimeter_ratio = nucleus_area / nucleus_perimeter;
            nucleus_roundness = (float)(Math.Pow(nucleus_perimeter, 2) / (4 * Math.PI * nucleus_area));
            data_row[1] = nucleus_cytoplasm_ratio.ToString();
            data_row[2] = area_perimeter_ratio.ToString();
            data_row[3] = nucleus_roundness.ToString();
            nucleus_area_static = nucleus_area;

            Dictionary<Point, double> nucleus_points_toborder_distance_dictionary = new Dictionary<Point, double>();

            for (int i = 0; i < nucleus_inner_points_list.Count; i++)
            {
                double distance = 1000;
                double euclidean_distance = 0;
                int index = -1;

                for (int j = 0; j < nucleus_border_points_list.Count; j++)
                {
                    euclidean_distance = Math.Sqrt((Math.Pow(nucleus_inner_points_list[i].X - nucleus_border_points_list[j].X, 2)
                                            + Math.Pow(nucleus_inner_points_list[i].Y - nucleus_border_points_list[j].Y, 2)));

                    if (euclidean_distance < distance)
                    {
                        distance = euclidean_distance;
                        index = j;
                    }
                }

                nucleus_points_toborder_distance_dictionary.Add(nucleus_inner_points_list[i], distance);
            }

            double min = nucleus_points_toborder_distance_dictionary.Values.Min();
            double max = nucleus_points_toborder_distance_dictionary.Values.Max();
            byte[] nImg = (byte[])bmpInfo.ImageBytes.Clone();

            for (int j = 1; j < bmpInfo.ImageHeight - 1; j++)
            {
                for (int i = 1; i < bmpInfo.ImageWidth - 1; i++)
                {
                    byte bndPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + i];

                    if (bndPixel == 255)
                    {
                        byte upPixel = bmpInfo.ImageBytes[(j - 1) * bmpInfo.RowSizeBytes + i];
                        byte downPixel = bmpInfo.ImageBytes[(j + 1) * bmpInfo.RowSizeBytes + i];
                        byte leftPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + (i - 1)];
                        byte rightPixel = bmpInfo.ImageBytes[j * bmpInfo.RowSizeBytes + (i + 1)];

                        if (upPixel == 255 && downPixel == 255 && leftPixel == 255 && rightPixel == 255)
                        {
                            byte dst = (byte)((nucleus_points_toborder_distance_dictionary[new Point(i, j)] - min) * 255.0 / (max - min));
                            nImg[j * bmpInfo.RowSizeBytes + i] = dst;
                        }
                        else
                        {
                            nImg[j * bmpInfo.RowSizeBytes + i] = 0;
                        }

                    }
                    else
                    {
                        nImg[j * bmpInfo.RowSizeBytes + i] = 0;
                    }
                }
            }

            MomentInvariants(bmp.Width, bmp.Height, bmpInfo.RowSizeBytes, nImg, file_path);
        }

        //计算不变矩
        private static void MomentInvariants(int width, int height, int row, byte[] cellByte, string file_path)
        {
            double m00, m10, m01;//几何矩
            double xc, yc;//区域或对象中心
            double n20, n02, n11, n30, n12, n21, n03;//中心矩
            double h1, h2, h3, h4, h5, h6, h7;//不变矩

            m00 = m01 = m10 = 0;
            xc = yc = 0;
            n20 = n02 = n11 = n30 = n12 = n21 = n03 = 0;
            h1 = h2 = h3 = h4 = h5 = h6 = h7 = 0;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (cellByte[j * row + i] > 0)
                    {
                        m00 += cellByte[j * row + i];
                        m10 += i * cellByte[j * row + i];
                        m01 += j * cellByte[j * row + i];
                    }
                }
            }

            xc = m10 / m00;
            yc = m01 / m00;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (cellByte[j * row + i] > 0)//i
                    {
                        n20 += Math.Pow(i - xc, 2) * cellByte[j * row + i];
                        n02 += Math.Pow(j - yc, 2) * cellByte[j * row + i];
                        //n11 += (i - xc) * (j - yc) * cellByte[j * row + i];
                        //n30 += Math.Pow(i - xc, 3) * cellByte[j * row + i];
                        //n12 += (i - xc) * Math.Pow(j - yc, 2) * cellByte[j * row + i];
                        //n21 += Math.Pow(i - xc, 2) * (j - yc) * cellByte[j * row + i];
                        //n03 += Math.Pow(j - yc, 3) * cellByte[j * row + i];
                    }
                }
            }

            n20 = n20 / Math.Pow(m00, 2);
            n02 = n02 / Math.Pow(m00, 2);
            //n11 = n11 / Math.Pow(m00, 2);
            //n30 = n30 / Math.Pow(m00, 2.5);
            //n12 = n12 / Math.Pow(m00, 2.5);
            //n21 = n21 / Math.Pow(m00, 2.5);
            //n03 = n03 / Math.Pow(m00, 2.5);

            h1 = n20 + n02;
            //h2 = Math.Pow(n20 - n02, 2) + 4 * Math.Pow(n11, 2);
            //h3 = Math.Pow(n30 - 3 * n12, 2) + Math.Pow(3 * n21 - n03, 2);
            //h4 = Math.Pow(n30 + n12, 2) + Math.Pow(n21 + n03, 2);
            //h5 = (n30 - 3 * n12) * (n30 + n12) * (Math.Pow(n30 + n12, 2) - 3 * Math.Pow(n21 + n03, 2))
            //    + (3 * n21 - n03) * (n21 + n03) * (3 * Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2));
            //h6 = (n20 - n02) * (Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2))
            //    + 4 * n11 * (n30 + n12) * (n21 + n03);
            //h7 = (3 * n21 - n03) * (n30 + n12) * (Math.Pow(n30 + n12, 2) - 3 * Math.Pow(n21 + n03, 2))
            //    + (3 * n12 - n30) * (n21 + n03) * (3 * Math.Pow(n30 + n12, 2) - Math.Pow(n21 + n03, 2));

            data_row[3] = h1.ToString();
        }

        //灰度共生矩阵，取角度为0,45,90,135,四种，然后取均值
        private static void Glcm(int Ng, int width, int height, int[] nucleus_bytes, float nucleus_area)
        {
            //a为在某点的像素值，b为a相邻的像素值，根据角度不同取值也不同，用于灰度共生矩阵计算
            int a, b;
            //灰度共生矩阵特性，能量，惯性矩，熵，局部同态性，相关度
            double energy = 0;
            double inertia = 0;
            double entropy = 0;
            double homogeneity = 0;
            double correlation = 0;
            //角度为0,45,90,135的灰度共生矩阵
            double[,] glcm0 = new double[Ng, Ng];
            double[,] glcm45 = new double[Ng, Ng];
            double[,] glcm90 = new double[Ng, Ng];
            double[,] glcm135 = new double[Ng, Ng];
            //margin probablity martix
            double[] glcm0X = new double[Ng];
            double[] glcm0Y = new double[Ng];
            double[] glcm45X = new double[Ng];
            double[] glcm45Y = new double[Ng];
            double[] glcm90X = new double[Ng];
            double[] glcm90Y = new double[Ng];
            double[] glcm135X = new double[Ng];
            double[] glcm135Y = new double[Ng];

            //量化到Ng个灰度值
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    //排除背景像素
                    if (nucleus_bytes[j * width + i] < 256)
                    {
                        for (int n = 1; n <= Ng; n++)
                        {
                            if (((n - 1) * 16 <= nucleus_bytes[j * width + i]) && (nucleus_bytes[j * width + i] <= (n - 1) * 16 + 15))
                            {
                                nucleus_bytes[j * width + i] = n - 1;
                            }
                        }
                    }
                }
            }

            //灰度共生矩阵GLCM计算,其中d=1，灰度级为Ng，角度分0，45,90,135
            //角度为0度
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    a = nucleus_bytes[j * width + i];
                    b = nucleus_bytes[j * width + i + 1];

                    //去除灰度256的像素
                    if (a < 256 && b < 256)
                    {
                        //灰度共生矩阵
                        glcm0[a, b] += 1;
                        //矩阵的转置
                        glcm0[b, a] = glcm0[a, b];
                    }
                }
            }

            //角度为45度
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    a = nucleus_bytes[j * width + i];
                    b = nucleus_bytes[(j + 1) * width + i + 1];

                    if (a < 256 && b < 256)
                    {
                        glcm45[a, b] += 1;
                        glcm45[b, a] = glcm45[a, b];
                    }
                }
            }

            //角度为90度
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    a = nucleus_bytes[j * width + i];
                    b = nucleus_bytes[(j + 1) * width + i];

                    if (a < 256 && b < 256)
                    {
                        glcm90[a, b] += 1;
                        glcm90[b, a] = glcm90[a, b];
                    }
                }
            }

            //角度为135度
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 3; i < width; i++)
                {
                    a = nucleus_bytes[j * width + i];
                    b = nucleus_bytes[(j + 1) * width + i - 1];

                    if (a < 256 && b < 256)
                    {
                        glcm135[a, b] += 1;
                        glcm135[b, a] = glcm135[a, b];
                    }
                }
            }

            //灰度共生矩阵归一化，由于为非规则图形，取面积
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    glcm0[i, j] /= nucleus_area;
                    glcm45[i, j] /= nucleus_area;
                    glcm90[i, j] /= nucleus_area;
                    glcm135[i, j] /= nucleus_area;
                }
            }
            //margin probablity martix
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    glcm0X[i] += glcm0[i, j];
                    glcm45X[i] += glcm45[i, j];
                    glcm90X[i] += glcm90[i, j];
                    glcm135X[i] += glcm135[i, j];
                }
            }
            for (int j = 0; j < Ng; j++)
            {
                for (int i = 0; i < Ng; i++)
                {
                    glcm0Y[j] += glcm0[i, j];
                    glcm45Y[j] += glcm45[i, j];
                    glcm90Y[j] += glcm90[i, j];
                    glcm135Y[j] += glcm135[i, j];
                }
            }

            //灰度共生矩阵特性：能量、熵、惯性矩、相关度计算
            //能量
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    energy = energy + Math.Pow(glcm0[i, j], 2) + Math.Pow(glcm45[i, j], 2) + Math.Pow(glcm90[i, j], 2) + Math.Pow(glcm135[i, j], 2);
                }
            }
            energy /= 4;

            //惯性矩
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    inertia += Math.Pow(i + 1 - j - 1, 2) * (glcm0[i, j] + glcm45[i, j] + glcm90[i, j] + glcm135[i, j]);
                }
            }
            inertia /= 4;

            //熵
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    entropy -= (glcm0[i, j] * Math.Log(glcm0[i, j] + 0.01, 2)
                                + glcm45[i, j] * Math.Log(glcm45[i, j] + 0.01, 2)
                                + glcm90[i, j] * Math.Log(glcm90[i, j] + 0.01, 2)
                                + glcm135[i, j] * Math.Log(glcm135[i, j] + 0.01, 2));
                }
            }
            entropy /= 4;

            //局部同态性
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    homogeneity = homogeneity + glcm0[i, j] / (1 + Math.Pow(i + 1 - j - 1, 2))
                                    + glcm45[i, j] / (1 + Math.Pow(i + 1 - j - 1, 2))
                                    + glcm90[i, j] / (1 + Math.Pow(i + 1 - j - 1, 2))
                                    + glcm135[i, j] / (1 + Math.Pow(i + 1 - j - 1, 2));
                }
            }
            homogeneity /= 4;

            //相关度
            //在不同角度下x，y两个方向的均值
            double avr0x = 0;
            double avr45x = 0;
            double avr90x = 0;
            double avr135x = 0;
            double avr0y = 0;
            double avr45y = 0;
            double avr90y = 0;
            double avr135y = 0;

            double avr0 = 0;
            double avr45 = 0;
            double avr90 = 0;
            double avr135 = 0;

            //在不同角度下x，y两个方向的方差
            double std0x = 0;
            double std45x = 0;
            double std90x = 0;
            double std135x = 0;
            double std0y = 0;
            double std45y = 0;
            double std90y = 0;
            double std135y = 0;

            //均值
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    avr0 += glcm0[i, j];
                    avr45 += glcm45[i, j];
                    avr90 += glcm90[i, j];
                    avr135 += glcm135[i, j];
                }
            }
            avr0 /= (Ng * Ng);
            avr45 /= (Ng * Ng);
            avr90 /= (Ng * Ng);
            avr135 /= (Ng * Ng);

            for (int i = 0; i < Ng; i++)
            {
                avr0x += glcm0X[i];
                avr45x += glcm45X[i];
                avr90x += glcm90X[i];
                avr135x += glcm135X[i];

                avr0y += glcm0Y[i];
                avr45y += glcm45Y[i];
                avr90y += glcm90Y[i];
                avr135y += glcm135Y[i];
            }
            avr0x /= Ng;
            avr0y /= Ng;
            avr45x /= Ng;
            avr45y /= Ng;
            avr90x /= Ng;
            avr90y /= Ng;
            avr135x /= Ng;
            avr135y /= Ng;

            //方差
            for (int i = 0; i < Ng; i++)
            {
                std0x += Math.Pow(glcm0X[i] - avr0x, 2);
                std0y += Math.Pow(glcm0Y[i] - avr0y, 2);
                std45x += Math.Pow(glcm45X[i] - avr45x, 2);
                std45y += Math.Pow(glcm45Y[i] - avr45y, 2);
                std90x += Math.Pow(glcm90X[i] - avr90x, 2);
                std90y += Math.Pow(glcm90Y[i] - avr90y, 2);
                std135x += Math.Pow(glcm135X[i] - avr135x, 2);
                std135y += Math.Pow(glcm135Y[i] - avr135y, 2);
            }
            std0x = Math.Sqrt(std0x / Ng);
            std0y = Math.Sqrt(std0y / Ng);
            std45x = Math.Sqrt(std45x / Ng);
            std45y = Math.Sqrt(std45y / Ng);
            std90x = Math.Sqrt(std90x / Ng);
            std90y = Math.Sqrt(std90y / Ng);
            std135x = Math.Sqrt(std135x / Ng);
            std135y = Math.Sqrt(std135y / Ng);

            //相关度
            for (int i = 0; i < Ng; i++)
            {
                for (int j = 0; j < Ng; j++)
                {
                    correlation = correlation
                        + ((i + 1) * (j + 1) * glcm0[i, j] - avr0x * avr0y) / (std0x * std0y)
                        + ((i + 1) * (j + 1) * glcm45[i, j] - avr45x * avr45y) / (std45x * std45y)
                        + ((i + 1) * (j + 1) * glcm90[i, j] - avr90x * avr90y) / (std90x * std90y)
                        + ((i + 1) * (j + 1) * glcm135[i, j] - avr135x * avr135y) / (std135x * std135y);
                }
            }
            correlation /= 4;
            //////////////////////////////////////////////////////////////////////////////////

            for (int i = 1; i < data_row.Length; i++)
            {
                if (data_row[i] == null)
                {
                    data_row[i] = energy.ToString();
                    data_row[i + 1] = inertia.ToString();
                    data_row[i + 2] = entropy.ToString();
                    data_row[i + 3] = homogeneity.ToString();
                    data_row[i + 4] = correlation.ToString();

                    break;
                }
            }
        }

        private static string GetCellTypeByFeatures()
        {
            double[] features = new double[3];
            double area_perimeter_ratio_classify = 0.0012127;//0.001213
            double nucleus_cytoplasm_ratio = double.Parse(data_row[1]);
            double area_perimeter_ratio = double.Parse(data_row[2]);
            double moment_first_order = double.Parse(data_row[3]);

            features[0] = 1.1533 * nucleus_cytoplasm_ratio + 10.8142 * area_perimeter_ratio - 207.7721;
            features[1] = -0.0001 * moment_first_order + 1.6115 * nucleus_cytoplasm_ratio - 3.0691;
            features[2] = -0.0016 * moment_first_order + 10.8142 * area_perimeter_ratio - 207.7721;

            if (features[0] > 0)
            {
                if (features[1] > 0)
                {
                    //嗜碱性细胞
                    return "basophils";
                }
                else
                {
                    if (features[2] > area_perimeter_ratio_classify)
                    {
                        //单核细胞
                        return @"monocytes";
                    }
                    else
                    {
                        //淋巴细胞
                        return "lymphocytes";
                    }
                }
            }
            else
            {
                //嗜酸性和中性粒细胞
                return "eosinophils_and_neutrophils";
            }
        }

        private static void MoveFile(string dest_path, string src_path)
        {
            if (!System.IO.Directory.Exists(dest_path))
            {
                System.IO.Directory.CreateDirectory(dest_path);
            }

            //copy binary bmp
            string sourcePath = src_path;
            string destPath = dest_path + Path.GetFileName(src_path);
            System.IO.File.Copy(sourcePath, destPath, true);
            //copy color bmp
            sourcePath = Path.ChangeExtension(src_path, ".jpg");
            destPath = dest_path + Path.GetFileNameWithoutExtension(src_path) + ".jpg";
            System.IO.File.Copy(sourcePath, destPath, true);
        }

        public static void SVMClassification()
        {
            //读入训练问题
            Problem train = Problem.Read("TRAIN.txt");
            RangeTransform range1 = RangeTransform.Compute(train);
            train = Scaling.Scale(range1, train);

            Problem test = Problem.Read("TEST.txt");
            RangeTransform range2 = RangeTransform.Compute(test);
            test = Scaling.Scale(range2, test);

            //构造训练参数：C值-惩罚因子 Gamma值
            Parameter parameters = new Parameter();

            double C;
            double Gamma;

            //根据训练样本取得最佳的参数并存储在C值和Gamma值，并保存在params.txt文件中
            ParameterSelection.Grid(train, parameters, "params.txt", out C, out Gamma);
            parameters.C = C;
            parameters.Gamma = Gamma;

            //利用训练样本训练，得到分类model
            Model model = Training.Train(train, parameters);

            //对测试样本进行分类，并将结果存储在results.txt文件中
            Prediction.Predict(test, "results.txt", model, false);

            MoveSVMClassifiedFile();
        }

        private static void MoveSVMClassifiedFile()
        {
            StreamReader result_stream_reader = new StreamReader(@"results.txt");
            StreamReader picname_stream_reader = new StreamReader(@"picname.txt");
            
            string path_eosinophils = @"D:\leukocytes\eosinophils\";
            string path_neutrophils = @"D:\leukocytes\neutrophils\";
            if (!System.IO.Directory.Exists(path_eosinophils))
            {
                System.IO.Directory.CreateDirectory(path_eosinophils);
            }

            if (!System.IO.Directory.Exists(path_neutrophils))
            {
                System.IO.Directory.CreateDirectory(path_neutrophils);
            }

            string pic_name = "";
            string jpg_name = "";
            string png_name = "";
            string jpg_source_path = "";
            string png_source_path = "";
            string jpg_dest_path = "";
            string png_dest_path = "";
            while (!result_stream_reader.EndOfStream)
            {
                pic_name = picname_stream_reader.ReadLine();
                jpg_name = pic_name + ".jpg";
                jpg_source_path = @"D:\leukocytes\eosinophils_and_neutrophils\" + jpg_name;

                png_name = pic_name + ".png";
                png_source_path = @"D:\leukocytes\eosinophils_and_neutrophils\" + png_name;

                int re = int.Parse(result_stream_reader.ReadLine());
                if (re == -1)
                {
                    jpg_dest_path = path_eosinophils + jpg_name;
                    png_dest_path = path_eosinophils + png_name;
                }
                else if (re == 1)
                {
                    jpg_dest_path = path_neutrophils + jpg_name;
                    png_dest_path = path_neutrophils + png_name;
                }
                //copy color img
                System.IO.File.Copy(jpg_source_path, jpg_dest_path, true);
                //copy binary img
                System.IO.File.Copy(png_source_path, png_dest_path, true);
            }
        }
    }
}
