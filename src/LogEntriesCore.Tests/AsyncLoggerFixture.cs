﻿namespace LogEntriesCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using LogentriesCore;
    using Xunit;
    using Attribute = LogentriesCore.Attribute;

    public class TestLogger : AsyncLoggerBase
    {
        public void SetStream(Stream stream)
        {
            this.Stream = stream;
        }

        protected override void EnsureOpenConnection()
        {
        }

        protected override void CloseConnection()
        {
        }

        public override void Dispose()
        {
        }
    }

    public class AsyncLoggerFixture
    {
        private static Guid TestToken = new Guid("2bfbea1e-10c3-4419-bdad-7e6435882e1f");

        public AsyncLoggerFixture()
        {
            ConfigurationManager.AppSettings["Logentries.Token"] = null;
            Environment.SetEnvironmentVariable("Logentries.Token", null);
        }

        [Fact]
        public void ShouldGetKeyFromEnvironmentVariableWhenNotSpecifiedInAppConfigOrCloudConfig()
        {
            //Arrange
            Environment.SetEnvironmentVariable("Logentries.Token", TestToken.ToString());

            //Act
            var asyncLogger = new TestLogger();

            //Assert
            Assert.Equal(asyncLogger.Token, TestToken.ToString());
        }

        [Fact]
        public void ShouldWriteLineToStream()
        {
            //Arrange
            using (var ms = new MemoryStream())
            {
                ConfigurationManager.AppSettings["Logentries.Token"] = "2bfbea1e-10c3-4419-bdad-7e6435882e1d";
                var asyncLogger = new TestLogger();
                asyncLogger.SetStream(ms);

                //Act
                asyncLogger.AddLine("test");
                Thread.Sleep(100);

                //Assert
                Assert.Equal("2bfbea1e-10c3-4419-bdad-7e6435882e1d\x1b[30;47;0mtest\n", Encoding.UTF8.GetString(ms.ToArray()));
            }
        }
        
        [Fact]
        public void ShouldThrowExceptionWhenNoTokenSpecified()
        {
            ConfigurationManager.AppSettings["Logentries.Token"] = null;

            //Arrange
            using (var ms = new MemoryStream())
            {
                var asyncLogger = new TestLogger();
                asyncLogger.SetStream(ms);

                //Act, Assert
                Assert.Throws<ConfigurationErrorsException>(() => asyncLogger.AddLine("test"));
            }
        }


        [Fact]
        public void TokenFromPropertyShouldTakePrecedenceOverTokenFromConfig()
        {
            //Arrange
            using (var ms = new MemoryStream())
            {
                ConfigurationManager.AppSettings["Logentries.Token"] = "2bfbea1e-10c3-4419-bdad-7e6435882e1d";
                var asyncLogger = new TestLogger {Token = TestToken.ToString()};
                asyncLogger.SetStream(ms);

                //Act
                asyncLogger.AddLine("test");
                Thread.Sleep(100);

                //Assert
                Assert.Contains(TestToken.ToString(), Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public void TokenShouldBeLoadedFromConfig()
        {
            //Arrange
            using (var ms = new MemoryStream())
            {
                ConfigurationManager.AppSettings["Logentries.Token"] = "2bfbea1e-10c3-4419-bdad-7e6435882e1f";
                var asyncLogger = new TestLogger();
                asyncLogger.SetStream(ms);

                //Act
                asyncLogger.AddLine("test");
                Thread.Sleep(100);

                //Assert
                Assert.Contains(TestToken.ToString(), Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public void AllNewLinesShouldBeReplacedWithUnicodeLineSeperator()
        {
            //Arrange
            using (var ms = new MemoryStream())
            {
                ConfigurationManager.AppSettings["Logentries.Token"] = "2bfbea1e-10c3-4419-bdad-7e6435882e1d";
                var asyncLogger = new TestLogger();
                asyncLogger.SetStream(ms);

                //Act
                asyncLogger.AddLine("test\r\n\n\rtest");
                Thread.Sleep(100);

                //Assert
                Assert.Equal(3, Encoding.UTF8.GetString(ms.ToArray()).Count(c => c == '\u2028'));
            }
        }

        [Fact]
        public void ShouldGenerateCorrectAnsiEscapeCodesForStyle()
        {
            //Arrange
            var expected = new Dictionary<Style, string>
            {
                { Style.Default, "\x1b[30;47;0m" },
                { new Style { ForegroundColor = ForegroundColor.White, BackgroundColor = BackgroundColor.Red },  "\x1b[37;41;0m" },
                { new Style { Attributes = new List<Attribute> {Attribute.Bold, Attribute.Underline, }},  "\x1b[30;47;1;4m" },
                { new Style { BackgroundColor = BackgroundColor.Blue },  "\x1b[30;44;0m" }
            };

            //Act, Assert
            foreach (var test in expected)
            {
                Assert.Equal(test.Key.ToString(), test.Value);    
            }
        }
    }
}
