﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Glimpse.AspNet.Model;
using Glimpse.AspNet.Tab;
using Glimpse.Core.Extensibility;
using Glimpse.Test.Common;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Glimpse.Test.AspNet.Tab
{
    public class ServerShould
    {
        [Fact]
        public void HaveProperContextObjectType()
        {
            var server = new Server();

            Assert.Equal(typeof(HttpContextBase), server.RequestContextType);
        }

        [Fact]
        public void UseDefaultLifeCycleSupport()
        {
            var server = new Server();
            Assert.Equal(RuntimeEvent.EndRequest, server.ExecuteOn);
        }

        [Fact]
        public void BeNamedRequest()
        {
            var server = new Server();
            Assert.Equal("Server", server.Name);
        }

        [Fact]
        public void HaveADocumentationUri()
        {
            var server = new Server();

            Assert.False(string.IsNullOrWhiteSpace(server.DocumentationUri));
        }

        [Fact]
        public void ReturnData()
        {
            var serverVariables = new NameValueCollection { { "Name", "Value" }, { "Name1", "Value1" }, { "HTTP_TEST", "http_test_value" }, {"CERT_TEST", "cert_test_value"}, { "HTTPS_TEST", "https_test_value" } };

            var requestMock = new Mock<HttpRequestBase>();
            requestMock.Setup(r => r.ServerVariables).Returns(serverVariables);
            var httpBaseMock = new Mock<HttpContextBase>();
            httpBaseMock.Setup(c => c.Request).Returns(requestMock.Object);
            var contextMock = new Mock<ITabContext>();
            contextMock.Setup(c => c.GetRequestContext<HttpContextBase>()).Returns(httpBaseMock.Object);

            var server = new Server();
            var result = server.GetData(contextMock.Object) as ServerModel;

            Assert.NotNull(result);

            var otherVariables = result.OtherVariables;
            Assert.NotNull(otherVariables);
            Assert.Equal(otherVariables.Count(), 2);
            Assert.Equal(otherVariables["Name"], "Value");
            Assert.Equal(otherVariables["Name1"], "Value1");

            var httpVariables = result.HttpVariables;
            Assert.NotNull(httpVariables);
            Assert.Equal(httpVariables.Count(), 1);
            Assert.Equal(httpVariables["HTTP_TEST"], "http_test_value");

            var httpsVariables = result.HttpsVariables;
            Assert.NotNull(httpsVariables);
            Assert.Equal(httpsVariables.Count(), 1);
            Assert.Equal(httpsVariables["HTTPS_TEST"], "https_test_value");

            var certVariables = result.CertVariables;
            Assert.NotNull(certVariables);
            Assert.Equal(certVariables.Count(), 1);
            Assert.Equal(certVariables["CERT_TEST"], "cert_test_value");
        }
    }
}