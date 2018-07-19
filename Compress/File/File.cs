﻿using System;
using System.IO;
using Compress.Utils;
using FileInfo = RVIO.FileInfo;
using FileStream = RVIO.FileStream;

namespace Compress.File
{
    public class File : ICompress
    {
        private FileInfo _fileInfo;
        private Stream _inStream;
        private byte[] _CRC = null;

        public string ZipFilename => _fileInfo?.FullName ?? "";

        public long TimeStamp => _fileInfo?.LastWriteTime ?? 0;

        public ZipOpenType ZipOpen { get; private set; }


        public ZipStatus ZipStatus { get; private set; }

        public int LocalFilesCount()
        {
            return 1;
        }

        public string Filename(int i)
        {
            return ZipFilename;
        }

        public ulong UncompressedSize(int i)
        {
            return _fileInfo != null ? (ulong)_fileInfo.Length : 0;
        }

        public ulong? LocalHeader(int i)
        {
            return 0;
        }

        public ZipReturn FileStatus(int i)
        {
            return ZipReturn.ZipGood;
        }

        public byte[] CRC32(int i)
        {
            return _CRC;
        }

        public ZipReturn ZipFileCreate(string newFilename)
        {
            if (ZipOpen != ZipOpenType.Closed)
            {
                return ZipReturn.ZipFileAlreadyOpen;
            }

            DirUtil.CreateDirForFile(newFilename);
            _fileInfo = new FileInfo(newFilename);

            int errorCode = FileStream.OpenFileWrite(newFilename, out _inStream);
            if (errorCode != 0)
            {
                ZipFileClose();
                return ZipReturn.ZipErrorOpeningFile;
            }
            ZipOpen = ZipOpenType.OpenWrite;
            return ZipReturn.ZipGood;
        }


        public void ZipFileClose()
        {
            if (ZipOpen == ZipOpenType.Closed)
            {
                return;
            }

            if (ZipOpen == ZipOpenType.OpenRead)
            {
                if (_inStream != null)
                {
                    _inStream.Close();
                    _inStream.Dispose();
                }
                ZipOpen = ZipOpenType.Closed;
                return;
            }

            _inStream.Flush();
            _inStream.Close();
            _inStream.Dispose();
            _fileInfo = new FileInfo(_fileInfo.FullName);
            ZipOpen = ZipOpenType.Closed;
        }


        public ZipReturn ZipFileOpen(string newFilename, long timestamp, bool readHeaders)
        {
            ZipFileClose();
            ZipStatus = ZipStatus.None;
            _fileInfo = null;

            try
            {
                if (!RVIO.File.Exists(newFilename))
                {
                    ZipFileClose();
                    return ZipReturn.ZipErrorFileNotFound;
                }
                _fileInfo = new FileInfo(newFilename);
                if (timestamp != -1 && _fileInfo.LastWriteTime != timestamp)
                {
                    ZipFileClose();
                    return ZipReturn.ZipErrorTimeStamp;
                }
                int errorCode = FileStream.OpenFileRead(newFilename, out _inStream);
                if (errorCode != 0)
                {
                    ZipFileClose();
                    if (errorCode == 32)
                    {
                        return ZipReturn.ZipFileLocked;
                    }
                    return ZipReturn.ZipErrorOpeningFile;
                }
            }
            catch (PathTooLongException)
            {
                ZipFileClose();
                return ZipReturn.ZipFileNameToLong;
            }
            catch (IOException)
            {
                ZipFileClose();
                return ZipReturn.ZipErrorOpeningFile;
            }
            ZipOpen = ZipOpenType.OpenRead;

            if (!readHeaders)
            {
                return ZipReturn.ZipGood;
            }


            //return ZipFileReadHeaders();
            return ZipReturn.ZipGood;
        }


        public ZipReturn ZipFileOpen(byte[] zipBytes)
        {
            ZipFileClose();
            ZipStatus = ZipStatus.None;
            _fileInfo = null;
            _inStream = new MemoryStream(zipBytes);
            ZipOpen = ZipOpenType.OpenRead;

            //return ZipFileReadHeaders();
            return ZipReturn.ZipGood;
        }



        public void ZipFileAddDirectory()
        {
            throw new NotImplementedException();
        }

        public ZipReturn ZipFileCloseWriteStream(byte[] crc32)
        {
            _CRC = crc32;
            return ZipReturn.ZipGood;
        }

        public ZipReturn ZipFileRollBack()
        {
            throw new NotImplementedException();
        }

        public void ZipFileCloseFailed()
        {
            throw new NotImplementedException();
        }

        public ZipReturn ZipFileOpenReadStream(int index, out Stream stream, out ulong streamSize)
        {
            _inStream.Position = 0;
            stream = _inStream;
            streamSize = (ulong)_fileInfo.Length;
            return ZipReturn.ZipGood;
        }

        public ZipReturn ZipFileOpenWriteStream(bool raw, bool trrntzip, string filename, ulong uncompressedSize, ushort compressionMethod, out Stream stream)
        {
            _inStream.Position = 0;
            stream = _inStream;
            return ZipReturn.ZipGood;
        }

        public ZipReturn ZipFileCloseReadStream()
        {
            return ZipReturn.ZipGood;
        }




    }
}