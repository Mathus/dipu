// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Allors bvba">
//   Copyright 2002-2009 Allors bvba.
// 
// Dual Licensed under
//   a) the General Public Licence v3 (GPL)
//   b) the Allors License
// 
// The GPL License is included in the file gpl.txt.
// The Allors License is an addendum to your contract.
// 
// Allors Platform is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// For more information visit http://www.allors.com/legal
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Allors
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Xml;

    using Allors.Databases.Memory.IntegerId;
    using Allors.Domain;
    using Allors.Workspaces.Memory.LongId;

    using NUnit.Framework;

    using Configuration = Allors.Databases.Memory.IntegerId.Configuration;

    [SetUpFixture]
    public class Fixture
    {
        public static string MinimalXml { get; private set; }

        public static string DefaultXml { get; private set; }

        [SetUp]
        public void SetUp()
        {
            var configuration = new Configuration { ObjectFactory = Config.ObjectFactory, WorkspaceFactory = new WorkspaceFactory() };
            Config.Default = new Database(configuration);

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            var database = Config.Default;
            database.Init();

            using (var session = database.CreateSession())
            {
                try
                {
                    new Setup(session).Apply();

                    session.Commit();

                    using (var stringWriter = new StringWriter())
                    {
                        using (var writer = new XmlTextWriter(stringWriter))
                        {
                            database.Save(writer);
                            MinimalXml = stringWriter.ToString();
                        }
                    }

                    var singleton = Singleton.Instance(session);
                    singleton.Guest = new PersonBuilder(session)
                        .WithUserName(Users.GuestUserName)
                        .WithLastName("Guest")
                        .WithCreationDate(DateTime.MinValue)
                        .WithLastModifiedDate(DateTime.UtcNow)
                        .Build();

                    singleton.DefaultLocale = new Locales(session).FindBy(Locales.Meta.Name, "nl-NL");

                    var administrator = new PersonBuilder(session)
                        .WithUserName(Users.AdministratorUserName)
                        .WithLastName("Administrator")
                        .WithCreationDate(DateTime.MinValue)
                        .WithLastModifiedDate(DateTime.UtcNow)
                        .Build();

                    session.Derive(true);

                    var administrators = new UserGroups(session).Administrators;
                    administrators.AddMember(administrator);

                    session.Derive(true);
                    session.Commit();

                    using (var stringWriter = new StringWriter())
                    {
                        using (var writer = new XmlTextWriter(stringWriter))
                        {
                            database.Save(writer);
                            DefaultXml = stringWriter.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                    throw;
                }
            }
        }
    }
}