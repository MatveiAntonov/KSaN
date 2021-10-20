using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace file_storage.Controllers
{
    [Route("file_storage")]
    [ApiController]
    public class FileStorageController : ControllerBase
    {
        private readonly ILogger<FileStorageController> _logger;
        private readonly string _path = @"D:\BSUIR\4 sem\KSaN\KSAN\lab5\store";

        public FileStorageController(ILogger<FileStorageController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{*filename}")]
        public ActionResult HTTP_GET(string filename)
        {
            if (filename == null)
            {
                filename = "";
            }
            if (File_check(filename))
            {
                try
                {
                    string path = Path.Combine(_path, filename);
                    FileStream file = new FileStream(path, FileMode.Open);
                    return File(file, "application/sometype", Path.GetFileName(filename));
                }
                catch
                {
                    return StatusCode(500);
                }
            }
            else
            {
                string directoryname = filename;
                try
                {
                    IReadOnlyCollection<string> files = FileSystem.GetFiles(Path.Combine(_path, directoryname));
                    IReadOnlyCollection<string> folders = FileSystem.GetDirectories(Path.Combine(_path, directoryname));
                    List<Element> content = new List<Element>();
                    foreach (var item in folders)
                    {
                        content.Add(new Element(Path.GetFileName(item), "Folder"));
                    }
                    foreach (var item in files)
                    {
                        content.Add(new Element(Path.GetFileName(item), "File"));
                    }
                    return new JsonResult(content, new JsonSerializerOptions { });
                }
                catch
                {
                    return NotFound();
                }

            }
        }

        [HttpHead("{*filename}")]
        public ActionResult HTTP_HEAD(string filename)
        {
            try
            {
                string Path = FileSystem.GetFileInfo(System.IO.Path.Combine(_path, filename)).ToString();
                FileInfo fileInfo = new FileInfo(Path);
                if (fileInfo.Exists)
                {
                    Response.Headers.Add("Name", fileInfo.Name);
                    // Response.Headers.Add("Path", fileInfo.DirectoryName);
                    Response.Headers.Add("Creation-date", fileInfo.CreationTime.ToString());
                    Response.Headers.Add("Changed-date", fileInfo.LastWriteTime.ToString());
                    Response.Headers.Add("Size", fileInfo.Length.ToString() + " Bytes");
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("{*filename}")]
        public ActionResult HTTP_DELETE(string filename)
        {
            try
            {
                if (File_check(filename))
                {
                    FileSystem.DeleteFile(Path.Combine(_path, filename));
                }
                else
                {
                    FileSystem.DeleteDirectory(Path.Combine(_path, filename), DeleteDirectoryOption.DeleteAllContents);
                }
                return Ok();
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPut("{*path}")]
        public ActionResult HTTP_PUT(string path)
        {
            if (path == null)
            {
                path = "";
            }
            IFormFileCollection formFiles;
            try
            {
                formFiles = Request.Form.Files;
            }
            catch
            {
                formFiles = null;
            }
            bool isCopyFile = Request.Headers.ContainsKey("X-Copy-From");
            string pathToFile;
            if (!isCopyFile && formFiles != null)
            {
                return UPLOAD(formFiles, path);
            }
            else if (isCopyFile)
            {
                pathToFile = Request.Headers["X-Copy-From"];
                return COPY(pathToFile, path);
            }
            else
            {
                return BadRequest();
            }
        }

        private ActionResult UPLOAD(IFormFileCollection Files, string path)
        {
            int count = 0;
            string pathTo = Path.Combine(_path, path);
            if (Directory.Exists(pathTo))
            {
                foreach (var file in Files)
                {
                    try
                    {
                        using (var fileStream = new FileStream(Path.Combine(pathTo, file.FileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }
                        count++;
                    }
                    catch { }
                }
                if (count == 0)
                {
                    return StatusCode(500);
                }
                else 
                {
                    return Ok();
                }
            }
            else
            {
                return BadRequest();
            }
        }

        private ActionResult COPY(string From, string To)
        {
            string pathFrom = Path.Combine(_path, From);
            string pathTo = Path.Combine(_path, To);
            if (File_check(pathFrom))
            {
                string fileName = Path.GetFileName(pathFrom);
                if (Directory.Exists(pathTo))
                {
                    try
                    {
                        using (var fileStream = new FileStream(Path.Combine(pathTo, fileName), FileMode.Create))
                        {
                            using (var fromCopyStream = new FileStream(pathFrom, FileMode.Open))
                            {
                                fromCopyStream.CopyTo(fileStream);
                            }
                        }
                    }
                    catch
                    {
                        return StatusCode(500);
                    }
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                return NotFound();
            }
        }

       /* private bool File_check(string str)
        {
            if ())
            {
                return System.IO.File.Exists(Path.Combine(_path, str);
            }
            else
            {
                return false;
            }

        }*/
    }
}
