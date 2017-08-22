# LDAP client library for .NET Standard 1.3

Supported on the .NET Standard 1.3 (https://docs.microsoft.com/en-us/dotnet/articles/standard/library) compatible .NET runtimes: .NET Core, .NET Framework 4.6, Universal Windows Platform, Xamarin.

It works with any LDAP protocol compatible directory server (including Microsoft Active Directory).

[![Build Status](https://travis-ci.org/solomon87/Novell.Directory.Ldap.NETStandard.svg?branch=master)](https://travis-ci.org/solomon87/Novell.Directory.Ldap.NETStandard) - Linux Build (includes functional tests & stress tests running against OpenLDAP) <br />

The library is originaly coming from Novell (https://www.novell.com/developer/ndk/ldap_libraries_for_c_sharp.html) - really old code base - looks like a tool-based conversion from Java - this is the original java code repo http://www.openldap.org/devel/gitweb.cgi?p=openldap-jldap.git;a=summary (first commit in that repo is from 2000 :)) - which explains some of the weirdness of the code base.

The Novell documentation for the original library:
* html: https://www.novell.com/documentation/developer/ldapcsharp/?page=/documentation/developer/ldapcsharp/cnet/data/front.html
* pdf: https://www.novell.com/documentation/developer/ldapcsharp/pdfdoc/cnet/cnet.pdf

There are a number of basic functional tests which are also run as stress tests (e.g. the functional tests running on multiple threads) running against OpenLDAP on Ubuntu Trusty.

Sample usage

```cs
using (var cn = new LdapConnection())
{
	// connect
	cn.Connect("<<hostname>>", 389);
	// bind with an username and password
	// this how you can verify the password of an user
	cn.Bind("<<userdn>>", "<<userpassword>>");
	// call ldap op
	// cn.Delete("<<userdn>>")
	// cn.Add(<<ldapEntryInstance>>)
}

```

Contributions and bugs reports are welcome.

The library has some samples which included in the solution items and are in the original state (see original_samples folder) - they may or may not compile on .NET Standard.
