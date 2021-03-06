﻿// -*- mode: csharp; encoding: utf-8; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: nil; -*-
// vim:set ft=cs fenc=utf-8 ts=4 sw=4 sts=4 et:
// $Id: a7db7754a428ebb526d000b64ab4e48866a29032 $
/* LinxFramework
 *   Practical class library based on Linx Core Library
 *   Part of Linx
 * Linx
 *   Library that Integrates .NET with eXtremes
 * Copyright © 2008-2010 Takeshi KIRIYA (aka takeshik) <takeshik@users.sf.net>
 * All rights reserved.
 * 
 * This file is part of LinxFramework.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace XSpect
{
    public class ExceptionHandler
        : Object
    {
        private String _indent;

        public Exception Exception
        {
            get;
            private set;
        }

        public ExceptionHandler(Exception ex)
        {
            this._indent = String.Empty;
            this.Exception = ex;
        }

        public virtual String GetDiagnosticMessage()
        {
            return String.Format(
            #region Here Document
@"Global Exception Handler Information
    at {0}

{1}
{2}
{3}
",
            #endregion
 String.Format(DateTime.Now.ToLocalTime().ToString("O")),
                this.GetSystemInformation(),
                this.GetExceptionInformation(this.Exception),
                this.GetDefaultAppDomainInformation()
            );
        }

        protected void Indent(Int32 n)
        {
            this._indent += new String(' ', 4 * n);
        }

        protected void Unindent(Int32 n)
        {
            this._indent = this._indent.Remove(0, 4 * n);
        }

        protected virtual String GetSystemInformation()
        {
            this.Indent(1);
            Process proc = Process.GetCurrentProcess();
            String systemInfo = String.Format(
            #region Here Document
@"SystemInformation:
{0}OperatingSystem = {1}
{0}RuntimeVersion = {2}
{0}ProcessorCount = {3}
{0}Process = {4}
{0}Uptime = {5}
",
            #endregion
 this._indent,
                Environment.OSVersion.VersionString,
                Environment.Version,
                Environment.ProcessorCount,
                String.Format(
                    "[{0}] {1} (credential: {2}@{3} uptime: {4})",
                    proc.Id,
                    proc.MainModule.ModuleName,
                    Environment.UserName,
                    Environment.UserDomainName,
                    DateTime.Now - proc.StartTime
                ),
                new TimeSpan((long) Environment.TickCount * 10000)
            );
            this.Unindent(1);
            return systemInfo;
        }

        protected virtual String GetExceptionInformation(Exception exception)
        {
            String exceptionInfo = "ExceptionStack:\r\n";

            IEnumerable<Exception> exceptions = new Exception[] { exception, };
            while (exceptions.Last().InnerException != null)
            {
                exceptions = exceptions.Concat(new Exception[] { exception.InnerException, });
            }
            exceptions = exceptions.Reverse();

            this.Indent(1);
            foreach (Exception ex in exceptions)
            {
                exceptionInfo += String.Format(
                    "{0}{1}{2}:\r\n",
                    this._indent,
                    !ex.GetType().IsGenericType
                        ? String.Format("[{0}] ", ex.GetType().Assembly.GetName().Name)
                        : String.Empty
                    ,
                    ex.GetType().FullName
                );
                this.Indent(1);
                if (!String.IsNullOrEmpty(ex.Message))
                {
                    exceptionInfo += String.Format(
                        "{0}Message = {1}\r\n",
                        this._indent,
                        ex.Message
                    );
                }
                if (ex.Data != null && ex.Data.Count > 0)
                {
                    exceptionInfo += String.Format(
                        "{0}Data:\r\n",
                        this._indent
                    );
                    foreach (String key in ex.Data.Keys)
                    {
                        foreach (String value in ex.Data.Values)
                        {
                            exceptionInfo += String.Format(
                                "{0}{1} = {2}\r\n",
                                key,
                                value
                            );
                        }
                    }
                }
                if (!String.IsNullOrEmpty(ex.HelpLink))
                {
                    exceptionInfo += String.Format(
                        "{0}HelpLink = {1}\r\n",
                        this._indent,
                        ex.HelpLink
                    );
                }
                if (!String.IsNullOrEmpty(ex.Source))
                {
                    exceptionInfo += String.Format(
                        "{0}Source = {1}\r\n",
                        this._indent,
                        ex.Source
                    );
                }
                if (ex.TargetSite != null)
                {
                    exceptionInfo += String.Format(
                        "{0}TargetSite = {1}\r\n",
                        this._indent,
                        ex.TargetSite
                    );
                }
                exceptionInfo += String.Format(
                    "{0}StackTrace:\r\n{0}{1}",
                    this._indent,
                    this.Exception.StackTrace
                );
            }
            this.Unindent(1);
            return exceptionInfo;
        }

        protected virtual String GetDefaultAppDomainInformation()
        {
            String appDomainInfo = "CurrentAppDomain:\r\n";

            this.Indent(1);
            appDomainInfo += String.Format(
                "{0}LoadedAssemblies:\r\n",
                this._indent
            );
            this.Indent(1);
            AppDomain domain = AppDomain.CurrentDomain;
            foreach (Assembly assembly in domain.GetAssemblies())
            {
                appDomainInfo += String.Format(
                    "{0}{1}, ProcessorArchitecture={2}\r\n",
                    this._indent,
                    assembly.GetName().ToString(),
                    Enum.GetName(typeof(ProcessorArchitecture), assembly.GetName().ProcessorArchitecture).ToLower()
                );
                this.Indent(1);

                if (!String.IsNullOrEmpty(assembly.CodeBase))
                {
                    appDomainInfo += String.Format(
                        "{0}CodeBase = {1}\r\n",
                        this._indent,
                        assembly.CodeBase
                    );
                }
                appDomainInfo += String.Format(
                    "{0}GlobalAssemblyCache = {1}\r\n",
                    this._indent,
                    assembly.GlobalAssemblyCache.ToString().ToLower()
                );
                if (File.Exists(assembly.Location))
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                    if (!String.IsNullOrEmpty(versionInfo.FileVersion))
                    {
                        appDomainInfo += String.Format(
                            "{0}FileVersion = {1}\r\n",
                            this._indent,
                            versionInfo.FileVersion
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.Comments))
                    {
                        appDomainInfo += String.Format(
                            "{0}Comments = {1}\r\n",
                            this._indent,
                            versionInfo.Comments
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.CompanyName))
                    {
                        appDomainInfo += String.Format(
                            "{0}CompanyName = {1}\r\n",
                            this._indent,
                            versionInfo.CompanyName
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.FileDescription))
                    {
                        appDomainInfo += String.Format(
                            "{0}FileDescription = {1}\r\n",
                            this._indent,
                            versionInfo.FileDescription
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.LegalCopyright))
                    {
                        appDomainInfo += String.Format(
                            "{0}LegalCopyright = {1}\r\n",
                            this._indent,
                            versionInfo.LegalCopyright
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.LegalTrademarks))
                    {
                        appDomainInfo += String.Format(
                            "{0}LegalTrademarks = {1}\r\n",
                            this._indent,
                            versionInfo.LegalTrademarks
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.Language))
                    {
                        appDomainInfo += String.Format(
                            "{0}Language = {1}\r\n",
                            this._indent,
                            versionInfo.Language
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.InternalName))
                    {
                        appDomainInfo += String.Format(
                            "{0}InternalName = {1}\r\n",
                            this._indent,
                            versionInfo.InternalName
                        );
                    }
                    if (!String.IsNullOrEmpty(versionInfo.OriginalFilename))
                    {
                        appDomainInfo += String.Format(
                            "{0}OriginalFileName = {1}\r\n",
                            this._indent,
                            versionInfo.OriginalFilename
                        );
                    }
                }
                this.Unindent(1);
            }
            this.Unindent(2);
            return appDomainInfo;
        }
    }
}
