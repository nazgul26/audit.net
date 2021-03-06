﻿// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    internal class ApiClient : ApiClientBase, IApiClient
    {
        private int _pageSize = 100;

        public ApiClient()
            : this(new HttpRequestCachePolicy(HttpRequestCacheLevel.Default))
        { }

        public ApiClient(HttpRequestCachePolicy cachePolicy)
            : base("https://ossindex.net/v2.0", cachePolicy)
        { }

        private void BeforeSerialization(IRestResponse response)
        {
            if (response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new ApiClientException(string.Format("Unexpected response status {0}", (int)response.StatusCode));
            }
        }

        public IList<Package> SearchPackages(IEnumerable<PackageSearch> searches)
        {
            var result = new List<Package>(searches.Count());

            while (searches.Any())
            {
                var request = new RestRequest(Method.POST);

                request.Resource = "package";
                request.RequestFormat = DataFormat.Json;
                request.OnBeforeDeserialization = BeforeSerialization;
                request.AddBody(searches.Take(this._pageSize));

                var response = Execute<PackageResponse>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
                }

                result.AddRange(response.Data);

                searches = searches.Skip(this._pageSize);
            }

            return result;
        }

        public IList<Package> SearchPackage(PackageSearch search)
        {
            return SearchPackage(search.pm, search.name, search.version);
        }

        public IList<Package> SearchPackage(string pm, string name, string version)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "package/{pm}/{name}/{version}";
            request.RequestFormat = DataFormat.Json;
            request.OnBeforeDeserialization = BeforeSerialization;

            request.AddParameter("pm", pm, ParameterType.UrlSegment);
            request.AddParameter("name", name, ParameterType.UrlSegment);
            request.AddParameter("version", version, ParameterType.UrlSegment);

            var response = Execute<PackageResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }
    }
}
