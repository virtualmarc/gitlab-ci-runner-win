using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gitlab_ci_runner.helper
{
    class TextFile
    {
        ///<summary>
        /// Get the content of the file
        ///</summary>
        ///<param name="sFilename">File path and name</param>
        public static string ReadFile(String sFilename)
        {
            string sContent = "";
            if (File.Exists(sFilename))
            {
                StreamReader myFile = new StreamReader(sFilename, System.Text.Encoding.Default);
                sContent = myFile.ReadToEnd();
                myFile.Close();
            }
            return sContent;
        }

        ///<summary>
        /// Writes a file
        ///</summary>
        ///<param name="sFilename">File path and name</param>
        ///<param name="sLines">Content</param>
        public static void WriteFile(String sFilename, String sLines)
        {
            StreamWriter myFile = new StreamWriter(sFilename);
            myFile.Write(sLines);
            myFile.Close();
        }

        ///<summary>
        /// Appends the file
        ///</summary>
        ///<param name="sFilename">File path and name</param>
        ///<param name="sLines">Content</param>
        public static void Append(string sFilename, string sLines)
        {
            StreamWriter myFile = new StreamWriter(sFilename, true);
            myFile.Write(sLines);
            myFile.Close();
        }

        ///<summary>
        /// Get the content of a defined line
        ///</summary>
        ///<param name="sFilename">File path and name</param>
        ///<param name="iLine">Line number</param>
        public static string ReadLine(String sFilename, int iLine)
        {
            string sContent = "";
            float fRow = 0;
            if (File.Exists(sFilename))
            {
                StreamReader myFile = new StreamReader(sFilename, System.Text.Encoding.Default);
                while (!myFile.EndOfStream && fRow < iLine)
                {
                    fRow++;
                    sContent = myFile.ReadLine();
                }
                myFile.Close();
                if (fRow < iLine)
                    sContent = "";
            }
            return sContent;
        }

        /// <summary>
        /// Writes into a defined line
        ///</summary>
        ///<param name="sFilename">File path and name</param>
        ///<param name="iLine">Line number</param>
        ///<param name="sLines">Content of the line</param>
        ///<param name="bReplace">Replace the lines content (true) or append (false)</param>
        public static void WriteLine(String sFilename, int iLine, string sLines, bool bReplace)
        {
            string sContent = "";
            string[] delimiterstring = { "\r\n" };
            if (File.Exists(sFilename))
            {
                StreamReader myFile = new StreamReader(sFilename, System.Text.Encoding.Default);
                sContent = myFile.ReadToEnd();
                myFile.Close();
            }
            string[] sCols = sContent.Split(delimiterstring, StringSplitOptions.None);
            if (sCols.Length >= iLine)
            {
                if (!bReplace)
                    sCols[iLine - 1] = sLines + "\r\n" + sCols[iLine - 1];
                else
                    sCols[iLine - 1] = sLines;
                sContent = "";
                for (int x = 0; x < sCols.Length - 1; x++)
                {
                    sContent += sCols[x] + "\r\n";
                }
                sContent += sCols[sCols.Length - 1];
            }
            else
            {
                for (int x = 0; x < iLine - sCols.Length; x++)
                    sContent += "\r\n";
                sContent += sLines;
            }
            StreamWriter mySaveFile = new StreamWriter(sFilename);
            mySaveFile.Write(sContent);
            mySaveFile.Close();
        }
    }
}
