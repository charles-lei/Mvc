// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ArrayModelBinderTest
    {
        [Fact]
        public async Task BindModelAsync_ValueProviderContainPrefix_Succeeds()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };
            var bindingContext = GetBindingContext(valueProvider);
            var modelState = bindingContext.ModelState;
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);

            int[] array = result.Model as int[];
            Assert.Equal(new[] { 42, 84 }, array);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task BindModelAsync_ValueProviderDoesNotContainPrefix_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(new SimpleHttpValueProvider());
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BindModelAsync_ModelMetadataReturnsReadOnly_ReturnsNull()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };
            var bindingContext = GetBindingContext(valueProvider, isReadOnly: true);
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 2)]
        public async Task BindModelAsync_BindingContextModelNonNull_FailsSilently(bool isReadOnly, int arrayLength)
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };

            var bindingContext = GetBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var array = new int[arrayLength];
            for (var i = 0; i < arrayLength; i++)
            {
                array[i] = 357;
            }
            bindingContext.Model = array;
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Same(array, result.Model);

            Assert.True(modelState.IsValid);
            for (var i = 0; i < arrayLength; i++)
            {
                // Array should be unchanged.
                Assert.Equal(357, array[i]);
            }
        }

        private static IModelBinder CreateIntBinder()
        {
            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(async (ModelBindingContext mbc) =>
                {
                    var value = await mbc.ValueProvider.GetValueAsync(mbc.ModelName);
                    if (value != null)
                    {
                        var model = value.ConvertTo(mbc.ModelType);
                        return new ModelBindingResult(model, key: null, isModelSet: true);
                    }
                    return null;
                });
            return mockIntBinder.Object;
        }

        private static ModelBindingContext GetBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<int[]>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(int[])),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateIntBinder(),
                    MetadataProvider = metadataProvider
                },
            };
            return bindingContext;
        }
    }
}
#endif
