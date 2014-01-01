using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace leukocytes_classification
{
    class ImageInfo
    {
        public struct ImageDataInfo
        {
            public Byte[] ImageBytes;   //存储图像像素数据的数组，字节型
            public Int32 ImageWidth;    //图像的宽
            public Int32 ImageHeight;   //图像的高
            public Int32 RowSizeBytes;  //图像每行字节数
            public Int32 totalSize;

            public ImageDataInfo(Int32 Width, Int32 Height, Int32 RowSizeBytes)
            {
                this.ImageWidth = Width;
                this.ImageHeight = Height;
                this.RowSizeBytes = RowSizeBytes;
                //分配内存
                this.totalSize = RowSizeBytes * Height;   //图像的总像素点
                ImageBytes = new byte[totalSize];        //定义图像像素的容量
            }
        }

        //得到Image的数据，返回ImageDataInfo结构.
        //bmp:源图像    PixelFormat:图像的像素格式
        public static ImageDataInfo GetImageBytes(Bitmap bmp, PixelFormat pixelFormat)
        {
            ImageDataInfo imgInfo = new ImageDataInfo();
            imgInfo.ImageWidth = bmp.Width;
            imgInfo.ImageHeight = bmp.Height;
            Rectangle rect = new Rectangle(0, 0, imgInfo.ImageWidth, imgInfo.ImageHeight);   //定图片的范围
            BitmapData imgData = bmp.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);

            imgInfo.RowSizeBytes = imgData.Stride;
            //分配内存
            imgInfo.totalSize = imgData.Stride * bmp.Height;
            imgInfo.ImageBytes = new byte[imgInfo.totalSize];
            //将数据复制到数组
            System.Runtime.InteropServices.Marshal.Copy(imgData.Scan0, imgInfo.ImageBytes, 0, imgInfo.totalSize);
            bmp.UnlockBits(imgData);  //解除锁定
            //释放内存
            imgData = null;
            //rect = null;

            return imgInfo;
        }

        //设置Image的数据，返回bmp图像
        //ImageDataInfo :图像数据    PixelFormat :图像的像素格式
        public static Bitmap SetImageBytes(ImageDataInfo imgInfo, PixelFormat pixelFormat)
        {
            Bitmap bmp = new Bitmap(imgInfo.ImageWidth, imgInfo.ImageHeight, pixelFormat);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData imgData = bmp.LockBits(rect, ImageLockMode.ReadWrite, pixelFormat);
            //分配内存
            int totalSize = imgData.Stride * imgData.Height;

            System.Runtime.InteropServices.Marshal.Copy(imgInfo.ImageBytes, 0, imgData.Scan0, totalSize);
            //解除锁定
            bmp.UnlockBits(imgData);
            //释放内存
            imgData = null;

            return bmp;
        }
    }
}
