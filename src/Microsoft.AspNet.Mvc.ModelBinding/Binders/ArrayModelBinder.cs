// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation for binding array values.
    /// </summary>
    /// <typeparam name="TElement">Type of elements in the array.</typeparam>
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        /// <inheritdoc />
        public override Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.Model == null && bindingContext.ModelMetadata.IsReadOnly)
            {
                return Task.FromResult<ModelBindingResult>(null);
            }

            return base.BindModelAsync(bindingContext);
        }

        /// <inheritdoc />
        protected override object GetModel(IEnumerable<TElement> newCollection)
        {
            if (newCollection == null)
            {
                return null;
            }

            return newCollection.ToArray();
        }

        /// <inheritdoc />
        protected override void CopyToModel([NotNull] object target, IEnumerable<TElement> sourceCollection)
        {
            TElement[] targetArray = target as TElement[];
            Debug.Assert(targetArray != null); // This binder is instantiated only for array model types.

            if (sourceCollection != null && targetArray != null)
            {
                int maxIndex = targetArray.Length - 1;
                int index = 0;
                foreach (var element in sourceCollection)
                {
                    if (index > maxIndex)
                    {
                        break;
                    }

                    targetArray[index++] = element;
                }
            }
            else
            {
                // Do not expect base implementation will succeed but just in case...
                base.CopyToModel(target, sourceCollection);
            }
        }
    }
}
