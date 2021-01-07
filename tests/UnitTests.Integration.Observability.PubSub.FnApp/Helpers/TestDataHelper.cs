using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnitTests.Integration.Observability.PubSub.FnApp.Helpers
{
    internal static class TestDataHelper
    {
        internal static string GetTestDataStringFromFile(string subfolder, string fileName)
        {
            // Gets the file path depending on the operating system
            string path = Path.Combine("TestData", subfolder, fileName);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {path}");
            }

            return File.ReadAllText(path);
        }
    }
}
