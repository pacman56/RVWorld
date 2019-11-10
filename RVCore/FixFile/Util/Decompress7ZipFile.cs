﻿using System.IO;
using Compress;
using Compress.SevenZip;
using RVCore.RvDB;
using FileStream = RVIO.FileStream;
using Path = RVIO.Path;
using FileInfo = RVIO.FileInfo;
using Directory = RVIO.Directory;

namespace RVCore.FixFile.Util
{
    public static class Decompress7ZipFile
    {
        private const int BufferSize = 128 * 4096;

        public static void DecompressSource7ZipFile(RvFile zZipFileIn, bool includeGood)
        {
            byte[] buffer = new byte[BufferSize];

            RvFile cacheDir = DB.RvFileCache();

            string fileNameIn = zZipFileIn.FullName;

            SevenZ zipFileIn = new SevenZ();
            ZipReturn zr1 = zipFileIn.ZipFileOpen(fileNameIn, zZipFileIn.TimeStamp, true);
            if (zr1 != ZipReturn.ZipGood)
                return;

            RvFile outDir = new RvFile(FileType.Dir)
            {
                Name = zZipFileIn.Name + ".cache",
                Parent = cacheDir,
                DatStatus = DatStatus.InToSort,
                GotStatus = GotStatus.Got
            };

            int nameDirIndex = 0;
            while (cacheDir.ChildNameSearch(outDir, out int index) == 0)
            {
                nameDirIndex++;
                outDir.Name = zZipFileIn.Name + ".cache (" + nameDirIndex + ")";
            }
            cacheDir.ChildAdd(outDir);
            Directory.CreateDirectory(outDir.FullName);

            for (int i = 0; i < zipFileIn.LocalFilesCount(); i++)
            {
                if (zZipFileIn.Child(i).IsDir)
                    continue;
                RvFile thisFile = null;
                for (int j = 0; j < zZipFileIn.ChildCount; j++)
                {
                    if (zZipFileIn.Child(j).ZipFileIndex != i)
                        continue;
                    thisFile = zZipFileIn.Child(j);
                }

                if (thisFile == null)
                    return;

                bool extract = true;

                // first check to see if we have a file  version of this compressed file somewhere else.
                foreach (RvFile f in thisFile.FileGroup.Files)
                {
                    if (f.FileType == FileType.File && f.GotStatus == GotStatus.Got)
                    {
                        extract = false;
                    }
                }
                if (!extract)
                    continue;


                extract = false;
                if (includeGood)
                {
                    // if this is the file we are fixing then pull out the correct files.
                    if (thisFile.RepStatus == RepStatus.Correct)
                        extract = true;
                }

                // next check to see if we need this extracted to fix another file
                foreach (RvFile f in thisFile.FileGroup.Files)
                {
                    if (f.RepStatus == RepStatus.CanBeFixed)
                    {
                        extract = true;
                        break;
                    }
                }

                if (!extract)
                    continue;

                string cleanedName = thisFile.Name;
                cleanedName = cleanedName.Replace("/", "-");
                cleanedName = cleanedName.Replace("\\", "-");

                RvFile outFile = new RvFile(FileType.File)
                {
                    Name = cleanedName,
                    Size = thisFile.Size,
                    CRC = thisFile.CRC,
                    SHA1 = thisFile.SHA1,
                    MD5 = thisFile.MD5,
                    HeaderFileType = thisFile.HeaderFileType,
                    AltSize = thisFile.AltSize,
                    AltCRC = thisFile.AltCRC,
                    AltSHA1 = thisFile.AltSHA1,
                    AltMD5 = thisFile.AltMD5,
                    FileGroup = thisFile.FileGroup
                };

                outFile.SetStatus(DatStatus.InToSort, GotStatus.Got);
                outFile.FileStatusSet(
                    FileStatus.HeaderFileTypeFromHeader |
                    FileStatus.SizeFromHeader | FileStatus.SizeVerified |
                    FileStatus.CRCFromHeader | FileStatus.CRCVerified |
                    FileStatus.SHA1FromHeader | FileStatus.SHA1Verified |
                    FileStatus.MD5FromHeader | FileStatus.MD5Verified |
                    FileStatus.AltSizeFromHeader | FileStatus.AltSizeVerified |
                    FileStatus.AltCRCFromHeader | FileStatus.AltCRCVerified |
                    FileStatus.AltSHA1FromHeader | FileStatus.AltSHA1Verified |
                    FileStatus.AltMD5FromHeader | FileStatus.AltMD5Verified
                    , thisFile);
                outFile.RepStatus = RepStatus.NeededForFix;

                zipFileIn.ZipFileOpenReadStream(i, out Stream readStream, out ulong unCompressedSize);

                string filenameOut = Path.Combine(outDir.FullName, outFile.Name);

                int errorCode = FileStream.OpenFileWrite(filenameOut, out Stream writeStream);

                ulong sizetogo = unCompressedSize;
                while (sizetogo > 0)
                {
                    int sizenow = sizetogo > BufferSize ? BufferSize : (int)sizetogo;

                    readStream.Read(buffer, 0, sizenow);

                    writeStream.Write(buffer, 0, sizenow);

                    sizetogo = sizetogo - (ulong)sizenow;
                }

                writeStream.Flush();
                writeStream.Close();
                writeStream.Dispose();

                FileInfo fi = new FileInfo(filenameOut);
                outFile.TimeStamp = fi.LastWriteTime;

                thisFile.FileGroup.Files.Add(outFile);

                outDir.ChildAdd(outFile);
            }

            zipFileIn.ZipFileClose();
        }

    }
}
