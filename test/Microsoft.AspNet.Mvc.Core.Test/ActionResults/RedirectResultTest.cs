// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class RedirectResultTest
    {
        [Fact]
        public void RedirectResult_Constructor_WithParameterUrl_SetsResultUrlAndNotPermanent()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url);

            // Assert
            Assert.False(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Fact]
        public void RedirectResult_Constructor_WithParameterUrlAndPermanent_SetsResultUrlAndPermanent()
        {
            // Arrange
            var url = "/test/url";

            // Act
            var result = new RedirectResult(url, permanent: true);

            // Assert
            Assert.True(result.Permanent);
            Assert.Same(url, result.Url);
        }

        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public void Execute_ReturnsContentPath_WhenItDoesNotStartWithTilde(string appRoot,
                                                                           string contentPath,
                                                                           string expectedPath)
        {
            // Arrange
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.Setup(o => o.Redirect(expectedPath, false))
                        .Verifiable();

            var httpContext = GetHttpContext(appRoot, contentPath, expectedPath, httpResponse.Object);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            httpResponse.Verify();
        }

        [Theory]
        [InlineData(null, "~/Home/About", "/Home/About")]
        [InlineData("/", "~/Home/About", "/Home/About")]
        [InlineData("/", "~/", "/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        public void Execute_ReturnsAppRelativePath_WhenItStartsWithTilde(string appRoot,
                                                                         string contentPath,
                                                                         string expectedPath)
        {
            // Arrange
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.Setup(o => o.Redirect(expectedPath, false))
                        .Verifiable();

            var httpContext = GetHttpContext(appRoot, contentPath, expectedPath, httpResponse.Object);
            var actionContext = GetActionContext(httpContext);
            var result = new RedirectResult(contentPath);

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            httpResponse.Verify();
        }

        [Fact]
        public void Execute_Calls_TempDataKeep()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>();
            tempData.Setup(t => t.Keep()).Verifiable();

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(o => o.Response).Returns(new Mock<HttpResponse>().Object);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(ITempDataDictionary))).Returns(tempData.Object);
            var actionContext = GetActionContext(httpContext.Object);

            var result = new RedirectResult("url")
            {
                UrlHelper = Mock.Of<IUrlHelper>()
            };

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            tempData.Verify(t => t.Keep(), Times.Once());
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(new Mock<IRouter>().Object);

            return new ActionContext(httpContext,
                                    routeData,
                                    new ActionDescriptor());
        }

        private static IServiceProvider GetServiceProvider(IUrlHelper urlHelper)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IUrlHelper>(urlHelper);
            serviceCollection.AddInstance<ITempDataDictionary>(Mock.Of<ITempDataDictionary>());
            return serviceCollection.BuildServiceProvider();
        }

        private static HttpContext GetHttpContext(string appRoot,
                                                     string contentPath,
                                                     string expectedPath,
                                                     HttpResponse response)
        {
            var httpContext = new Mock<HttpContext>();
            var actionContext = GetActionContext(httpContext.Object);
            var mockContentAccessor = new Mock<IScopedInstance<ActionContext>>();
            mockContentAccessor.SetupGet(o => o.Value).Returns(actionContext);
            var mockActionSelector = new Mock<IActionSelector>();
            var urlHelper = new UrlHelper(mockContentAccessor.Object, mockActionSelector.Object);
            var serviceProvider = GetServiceProvider(urlHelper);

            httpContext.Setup(o => o.Response)
                       .Returns(response);
            httpContext.SetupGet(o => o.RequestServices)
                       .Returns(serviceProvider);
            httpContext.Setup(o => o.Request.PathBase)
                       .Returns(new PathString(appRoot));

            return httpContext.Object;
        }
    }
}