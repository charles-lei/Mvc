// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class BodyValidationIntegrationTests
    {
        private class Person
        {
            [FromBody]
            [Required]
            public Address Address { get; set; }
        }

        private class Address
        {
            public string Street { get; set; }
        }

        [Fact]
        public async Task BodyBoundOnProperty_RequiredOnProperty_AddsModelStateError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var httpContext = operationContext.HttpContext;
            ConfigureHttpRequest(httpContext.Request, string.Empty);
            var modelState = new ModelStateDictionary();

            // Act
            var model = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.Equal("The Address field is required.", modelState[""].Errors.Single().ErrorMessage);
        }

        private class Person4
        {
            [FromBody]
            [Required]
            public int Address { get; set; }
        }

        [Fact]
        public async Task BodyBoundOnProperty_RequiredOnValueTypeProperty_AddsModelStateError()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person4)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var httpContext = operationContext.HttpContext;
            ConfigureHttpRequest(httpContext.Request, string.Empty);
            var modelState = new ModelStateDictionary();

            // Act
            var model = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.StartsWith("No JSON content found and type 'System.Int32' is not nullable.",
                modelState[""].Errors.Single().Exception.Message);
        }

        private class Person2
        {
            [FromBody]
            public Address2 Address { get; set; }
        }

        private class Address2
        {
            [Required]
            public string Street { get; set; }

            public int Zip { get; set; }
        }

        [Theory]
        [InlineData("{ \"Zip\" : 123 }")]
        [InlineData("{}")]
        public async Task BodyBoundOnTopLevelProperty_RequiredOnSubProperty_AddsModelStateError(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person2)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var httpContext = operationContext.HttpContext;
            ConfigureHttpRequest(httpContext.Request, inputText);
            var modelState = new ModelStateDictionary();

            // Act
            var model = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            Assert.Equal("The Street field is required.", modelState["Street"].Errors.Single().ErrorMessage);
        }

        private class Person3
        {
            [FromBody]
            public Address3 Address { get; set; }
        }

        private class Address3
        {
            public string Street { get; set; }

            [Required]
            public int Zip { get; set; }
        }

        [Theory]
        [InlineData("{ \"Street\" : \"someStreet\" }")]
        [InlineData("{}")]
        public async Task BodyBoundOnProperty_RequiredOnValueTypeSubProperty_AddsModelStateError(string inputText)
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person3)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext();
            var httpContext = operationContext.HttpContext;
            ConfigureHttpRequest(httpContext.Request, inputText);
            var actionContext = httpContext.RequestServices.GetRequiredService<IScopedInstance<ActionContext>>().Value;
            var modelState = actionContext.ModelState;

            // Act
            var model = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // TODO : This is a bug in how the input formatters add the error. Ideally the key should have been
            // Zip.
            Assert.StartsWith(
                "Required property 'Zip' not found in JSON. Path ''",
                modelState[""].Errors.Single().Exception.Message);
        }

        private static void ConfigureHttpRequest(HttpRequest request, string jsonContent)
        {
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            request.ContentType = "application/json";
        }
    }
}