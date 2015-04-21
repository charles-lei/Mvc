// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request headers when a model
    /// has the binding source <see cref="BindingSource.Header"/>/
    /// </summary>
    public class HeaderModelBinder : BindingSourceModelBinder
    {
        /// <summary>
        /// Creates a new <see cref="HeaderModelBinder"/>.
        /// </summary>
        public HeaderModelBinder()
            : base(BindingSource.Header)
        {
        }

        /// <inheritdoc />
        protected override Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
        {
            var httpContext = bindingContext.OperationBindingContext.HttpContext;
            var modelMetadata = bindingContext.ModelMetadata;
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<HeaderModelBinder>>();

            // Property name can be null if the model metadata represents a type (rather than a property or parameter).
            var headerName = bindingContext.BinderModelName ?? modelMetadata.PropertyName ?? bindingContext.ModelName;
            object model = null;
            if (bindingContext.ModelType == typeof(string))
            {
                var value = httpContext.Request.Headers.Get(headerName);
                if (value != null)
                {
                    model = value;
                }
            }
            else if (typeof(IEnumerable<string>).GetTypeInfo().IsAssignableFrom(
                bindingContext.ModelType.GetTypeInfo()))
            {
                var values = httpContext.Request.Headers.GetCommaSeparatedValues(headerName);
                if (values != null)
                {
                    model = ModelBindingHelper.ConvertValuesToCollectionType(
                        bindingContext.ModelType,
                        values);
                }
            }
            else
            {
                logger.LogVerbose(
                    "Unable to bind data for model with name '{ModelName}' and type '{ModelType}'." +
                    string.Format(
                        "Expected a model of type '{0}' or '{1}'.",
                        typeof(string).FullName,
                        typeof(IEnumerable<string>).FullName)
                    ,
                    headerName,
                    bindingContext.ModelType.FullName);
            }

            return Task.FromResult(new ModelBindingResult(model, bindingContext.ModelName, isModelSet: model != null));
        }
    }
}