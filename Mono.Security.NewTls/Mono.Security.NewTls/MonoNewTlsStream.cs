﻿//
// MonoNewTlsStream.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

extern alias NewSystemSource;
extern alias PrebuiltSystem;
using EncryptionPolicy = NewSystemSource::System.Net.Security.EncryptionPolicy;
using LocalCertificateSelectionCallback = NewSystemSource::System.Net.Security.LocalCertificateSelectionCallback;
using RemoteCertificateValidationCallback = NewSystemSource::System.Net.Security.RemoteCertificateValidationCallback;
using SslStream = NewSystemSource::System.Net.Security.SslStream;

using System;
using System.IO;
using System.Threading.Tasks;
#if PREBUILT_MSI
using MSI = PrebuiltSystem::Mono.Security.Interface;
#else
using MSI = Mono.Security.Interface;
#endif

using Mono.Security.NewTls;

namespace Mono.Security.Providers.NewTls
{
	public class MonoNewTlsStream : SslStream
	{
		internal MonoNewTlsStream (Stream innerStream, MSI.MonoTlsProvider provider, MSI.MonoTlsSettings settings)
			: this (innerStream, false, provider, settings)
		{
		}

		internal MonoNewTlsStream (Stream innerStream, bool leaveOpen, MSI.MonoTlsProvider provider, MSI.MonoTlsSettings settings)
			: base (innerStream, leaveOpen, EncryptionPolicy.RequireEncryption, provider, settings)
		{
		}

		new public bool IsClosed {
			get { return base.IsClosed; }
		}

		new public MSI.TlsException LastError {
			get { return (MSI.TlsException)base.LastError; }
		}

		public MSI.MonoTlsConnectionInfo GetConnectionInfo ()
		{
			var info = GetMonoConnectionInfo ();
			if (info == null)
				return null;
			return new MSI.MonoTlsConnectionInfo {
				CipherSuiteCode = (MSI.CipherSuiteCode)info.CipherSuiteCode,
				ProtocolVersion = (MSI.TlsProtocols)info.ProtocolVersion
			};
		}

		public Task Shutdown ()
		{
			return Task.Factory.FromAsync ((state, result) => BeginShutdown (state, result), EndShutdown, null);
		}

		public Task RequestRenegotiation ()
		{
			return Task.Factory.FromAsync ((state, result) => BeginRenegotiate (state, result), EndRenegotiate, null);
		}
	}
}


