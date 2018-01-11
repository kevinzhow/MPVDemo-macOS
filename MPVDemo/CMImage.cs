using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using Foundation;

namespace CMToolKit
{
    public class CMImage
    {
        public static bool SaveImageToFile(NSImage image, string filename)
        {
            var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                
            string jpgFilename = System.IO.Path.Combine(documentsDirectory, filename); // hardcoded filename, overwritten each time
            NSData imgData = image.AsTiff();
            NSError err = null;
                
            if (imgData.Save(jpgFilename, false, out err))
            {
                Console.WriteLine("saved as " + jpgFilename);
                return true;
            }
            else
            {
                Console.WriteLine("NOT saved as " + jpgFilename + " because" + err.LocalizedDescription);
                return false;
            }

        }

        public static NSImage MergeImages(List<NSImage> images)
        {
            var thumbnail_width = images.First().Size.Width;
            var imageHeight = images.First().Size.Height;
            var thumbnial_all_image = new NSImage(new CGSize(thumbnail_width*images.Count, imageHeight));
            
            thumbnial_all_image.LockFocus();
            var newImageRect = new CGRect(0,0,0,0);
            newImageRect.Size = thumbnial_all_image.Size;
                        
            int index = 0;
            foreach (var i in images)
            {
                i.DrawInRect(
                    new CGRect(thumbnail_width*index,0, thumbnail_width, imageHeight),
                    new CGRect(0,0, thumbnail_width, imageHeight),
                    NSCompositingOperation.SourceOver,
                    1 );
                ++index;
            }
            thumbnial_all_image.UnlockFocus();

            return thumbnial_all_image;
        }
    }

}