﻿//-----------------------------------------------------------------------
// <copyright file="BlobResponse.cs" company="Microsoft">
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
//    Contains code for the BlobResponse class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.StorageClient.Protocol
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Web;

    /// <summary>
    /// Provides a set of methods for parsing responses from blob operations.
    /// </summary>
    public static class BlobResponse
    {
        /// <summary>
        /// Returns extended error information from the storage service, that is in addition to the HTTP status code returned with the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>An object containing extended error information returned with the response.</returns>
        public static StorageExtendedErrorInformation GetError(HttpWebResponse response)
        {
            return Response.GetError(response);
        }

        /// <summary>
        /// Gets a collection of user-defined metadata from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A collection of user-defined metadata, as name-value pairs.</returns>
        public static NameValueCollection GetMetadata(HttpWebResponse response)
        {
            return Response.GetMetadata(response);
        }

        /// <summary>
        /// Gets an array of values for a specified name-value pair from a response that includes user-defined metadata.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <param name="name">The name associated with the metadata values to return.</param>
        /// <returns>An array of metadata values.</returns>
        public static string[] GetMetadata(HttpWebResponse response, string name)
        {
            return Response.GetMetadata(response, name);
        }

        /// <summary>
        /// Gets the blob's attributes, including its metadata and properties, from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The blob's attributes.</returns>
        public static BlobAttributes GetAttributes(HttpWebResponse response)
        {
            BlobAttributes attributes = new BlobAttributes();
            var properties = attributes.Properties = new BlobProperties();
            
            properties.CacheControl = response.Headers[HttpResponseHeader.CacheControl];
            properties.ContentEncoding = response.Headers[HttpResponseHeader.ContentEncoding];
            properties.ContentLanguage = response.Headers[HttpResponseHeader.ContentLanguage];
            properties.ContentMD5 = response.Headers[HttpResponseHeader.ContentMd5];
            properties.ContentType = response.Headers[HttpResponseHeader.ContentType];
            properties.ETag = Response.GetETag(response);
            properties.LastModifiedUtc = response.LastModified.ToUniversalTime();
            
            string blobType = response.Headers[Constants.HeaderConstants.BlobType];

            // Get blob type
            if (!string.IsNullOrEmpty(blobType))
            {
                properties.BlobType = (BlobType)Enum.Parse(typeof(BlobType), blobType, true);
            }

            // Get lease properties
            properties.LeaseStatus = Response.GetLeaseStatus(response);
            properties.LeaseState = Response.GetLeaseState(response);
            properties.LeaseDuration = Response.GetLeaseDuration(response);

            // Get the content length. Prioritize range and x-ms over content length for the special cases.
            var rangeHeader = response.Headers[HttpResponseHeader.ContentRange];
            var xContentLengthHeader = response.Headers[Constants.HeaderConstants.ContentLengthHeader];
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                properties.Length = long.Parse(rangeHeader.Split('/')[1]);
            }
            else if (!string.IsNullOrEmpty(xContentLengthHeader))
            {
                properties.Length = long.Parse(xContentLengthHeader);
            }
            else
            {
                properties.Length = response.ContentLength;
            }

            attributes.Uri = new Uri(response.ResponseUri.GetLeftPart(UriPartial.Path));
            
            var queryParameters = HttpUtility.ParseQueryString(response.ResponseUri.Query);

            DateTime snapshot;
            var snapshotString = queryParameters.Get(Constants.QueryConstants.Snapshot);
            if (!string.IsNullOrEmpty(snapshotString) && 
                    DateTime.TryParse(
                        snapshotString, 
                        System.Globalization.CultureInfo.InvariantCulture, 
                        System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out snapshot))
            {
                    attributes.Snapshot = snapshot;
            }

            attributes.Metadata = GetMetadata(response);

            // Get the copy attributes, if any
            CopyState copyAttributes = GetCopyAttributes(response);
            if (copyAttributes != null)
            {
                attributes.CopyState = copyAttributes;
            }

            return attributes;
        }

        /// <summary>
        /// Gets the request ID from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>A unique value associated with the request.</returns>
        public static string GetRequestId(HttpWebResponse response)
        {
            return Response.GetRequestId(response);
        }

        /// <summary>
        /// Gets the snapshot timestamp from the response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The snapshot timestamp.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "Storage only supports HTTP")]
        public static string GetSnapshotTime(HttpWebResponse response)
        {
            return response.Headers[Constants.HeaderConstants.SnapshotHeader];
        }

        /// <summary>
        /// Extracts the lease ID header from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The lease ID.</returns>
        public static string GetLeaseId(HttpWebResponse response)
        {
            return Response.GetLeaseId(response);
        }

        /// <summary>
        /// Extracts the remaining lease time from a web response.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The remaining lease time, in seconds.</returns>
        public static int? GetRemainingLeaseTime(HttpWebResponse response)
        {
            return Response.GetRemainingLeaseTime(response);
        }
        
        /// <summary>
        /// Parses the response for a blob listing operation.
        /// </summary>
        /// <param name="stream">The response stream.</param>
        /// <returns>An object that may be used for parsing data from the results of a blob listing operation.</returns>
        public static ListBlobsResponse List(Stream stream)
        {
            return new ListBlobsResponse(stream);
        }

        /// <summary>
        /// Parses the response for a blob listing operation.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>An object that may be used for parsing data from the results of a blob listing operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "Storage only supports HTTP")]
        public static ListBlobsResponse List(HttpWebResponse response)
        {
            return new ListBlobsResponse(response.GetResponseStream());
        }

        /// <summary>
        /// Parses the response for an operation that returns a block list for the blob.
        /// </summary>
        /// <param name="stream">The response stream.</param>
        /// <returns>An object that may be used for parsing data from the results of an operation to return a block list.</returns>
        public static GetBlockListResponse GetBlockList(Stream stream)
        {
            return new GetBlockListResponse(stream);
        }
        
        /// <summary>
        /// Parses the response for an operation that returns a block list for the blob.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>An object that may be used for parsing data from the results of an operation to return a block list.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Storage only supports HTTP")]
        public static GetBlockListResponse GetBlockList(HttpWebResponse response)
        {
            return new GetBlockListResponse(response.GetResponseStream());
        }

        /// <summary>
        /// Parses the response for an operation that returns a range of pages.
        /// </summary>
        /// <param name="stream">The response stream.</param>
        /// <returns>An object that may be used for parsing data from the results of an operation to return a range of pages.</returns>
        public static GetPageRangesResponse GetPageRanges(Stream stream)
        {
            return new GetPageRangesResponse(stream);
        }

        /// <summary>
        /// Parses the response for an operation that returns a range of pages.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>An object that may be used for parsing data from the results of an operation to return a range of pages.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Storage only supports HTTP")]
        public static GetPageRangesResponse GetPageRanges(HttpWebResponse response)
        {
            return new GetPageRangesResponse(response.GetResponseStream());
        }

        /// <summary>
        /// Reads service properties from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service properties.</param>
        /// <returns>The service properties stored in the stream.</returns>
        public static ServiceProperties ReadServiceProperties(Stream inputStream)
        {
            return Response.ReadServiceProperties(inputStream);
        }

        /// <summary>
        /// Extracts a <see cref="CopyState"/> object from the headers of a web response.
        /// </summary>
        /// <param name="response">The HTTP web response.</param>
        /// <returns>A <see cref="CopyState"/> object, or null if the web response does not contain a copy status.</returns>
        internal static CopyState GetCopyAttributes(HttpWebResponse response)
        {
            string copyStatusString = response.Headers[Constants.HeaderConstants.CopyStatusHeader];
            if (!string.IsNullOrEmpty(copyStatusString))
            {
                return GetCopyAttributes(
                    copyStatusString,
                    response.Headers[Constants.HeaderConstants.CopyIdHeader],
                    response.Headers[Constants.HeaderConstants.CopySourceHeader],
                    response.Headers[Constants.HeaderConstants.CopyProgressHeader],
                    response.Headers[Constants.HeaderConstants.CopyCompletionTimeHeader],
                    response.Headers[Constants.HeaderConstants.CopyDescriptionHeader]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a <see cref="CopyState"/> object from the given strings containing formatted copy information.
        /// </summary>
        /// <param name="copyStatusString">The copy status, as a string.</param>
        /// <param name="copyId">The copy ID.</param>
        /// <param name="copySourceString">The source URI of the copy, as a string.</param>
        /// <param name="copyProgressString">A string formatted as progressBytes/TotalBytes.</param>
        /// <param name="copyCompletionTimeString">The copy completion time, as a string, or null.</param>
        /// <param name="copyStatusDescription">The copy status description, if any.</param>
        /// <returns>A <see cref="CopyState"/> object populated from the given strings.</returns>
        internal static CopyState GetCopyAttributes(
            string copyStatusString,
            string copyId,
            string copySourceString,
            string copyProgressString,
            string copyCompletionTimeString,
            string copyStatusDescription)
        {
            CopyState copyAttributes = new CopyState
            {
                CopyId = copyId,
                StatusDescription = copyStatusDescription
            };

            switch (copyStatusString)
            {
                case Constants.CopySuccessValue:
                    copyAttributes.Status = CopyStatus.Success;
                    break;
                case Constants.CopyPendingValue:
                    copyAttributes.Status = CopyStatus.Pending;
                    break;
                case Constants.CopyAbortedValue:
                    copyAttributes.Status = CopyStatus.Aborted;
                    break;
                case Constants.CopyFailedValue:
                    copyAttributes.Status = CopyStatus.Failed;
                    break;
                default:
                    copyAttributes.Status = CopyStatus.Invalid;
                    break;
            }

            if (!string.IsNullOrEmpty(copyProgressString))
            {
                string[] progressSequence = copyProgressString.Split('/');
                copyAttributes.BytesCopied = long.Parse(progressSequence[0]);
                copyAttributes.TotalBytes = long.Parse(progressSequence[1]);
            }

            if (!string.IsNullOrEmpty(copySourceString))
            {
                copyAttributes.Source = new Uri(copySourceString);
            }

            if (!string.IsNullOrEmpty(copyCompletionTimeString))
            {
                copyAttributes.CompletionTime = copyCompletionTimeString.ToUTCTime();
            }

            return copyAttributes;
        }
    }
}
