using System;

namespace ExTools
{
    public static class FileExTool
    {
        public static bool IsTextFile(byte[] fileHeader)
        {
            // 简单判断是否为文本文件（检查BOM或ASCII范围字符）
            if (fileHeader.Length >= 3 &&
                fileHeader[0] == 0xEF && fileHeader[1] == 0xBB && fileHeader[2] == 0xBF)
                return true; // UTF-8 BOM

            if (fileHeader.Length >= 2 &&
                fileHeader[0] == 0xFF && fileHeader[1] == 0xFE)
                return true; // UTF-16 LE BOM

            if (fileHeader.Length >= 2 &&
                fileHeader[0] == 0xFE && fileHeader[1] == 0xFF)
                return true; // UTF-16 BE BOM

            // 检查前128个字节是否都在可打印ASCII范围内
            var checkLength = Math.Min(128, fileHeader.Length);
            var allAscii = true;

            for (var i = 0; i < checkLength; i++)
            {
                var b = fileHeader[i];
                if (b == 0) // 空结束符提前退出
                    break;

                if (b < 0x09 || (b > 0x0D && b < 0x20) || b > 0x7E)
                {
                    // 非可打印ASCII、制表符、换行符等
                    allAscii = false;
                    break;
                }
            }

            return allAscii;
        }
    }
}
