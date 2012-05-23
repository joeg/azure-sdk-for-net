﻿//-----------------------------------------------------------------------
// <copyright file="SharedAccessBlobPolicies.cs" company="Microsoft">
//    Copyright 2012 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <summary>
//    Contains code for the SharedAccessBlobPolicies class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.StorageClient
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the collection of shared access policies defined for a container.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Naming",
        "CA1710:IdentifiersShouldHaveCorrectSuffix",
        Justification = "Public APIs should not expose base collection types.")]
    public class SharedAccessBlobPolicies : Dictionary<string, SharedAccessBlobPolicy>
    {
    }
}