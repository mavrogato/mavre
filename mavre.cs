using System;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace Mavre
{
    static class Entry {
        static IEnumerable<XDocument> Load(IEnumerable<Uri> uris) {
            List<XDocument> ret = new List<XDocument>{};
            Action<Uri> LoadRec = null;
            LoadRec = (Uri uri) => {
                if (null != ret && ret.Any(x => new Uri(x.BaseUri) == uri)) return;
                var doc = XDocument.Load(uri.ToString(), LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);
                if (null == doc.Element("mavlink")) {
                    throw new Exception(string.Format("{0}:1: does not contain mavlink...", uri));
                }
                ret.Add(doc);
                foreach (var inc in doc.Elements("mavlink").Elements("include")) {
                    LoadRec(new Uri(uri, inc.Value));
                }
            };
            foreach (var uri in uris) LoadRec(uri);
            return ret;
        }

        static void PrintSourceList(this IEnumerable<XElement> elements) {
            foreach (var x in elements.GroupBy(x => x.BaseUri)) {
                Console.WriteLine("{0}\t{1}(s) loaded from {2}", x.Count(), x.First().Name, x.First().BaseUri);
            }
            Console.WriteLine("Total {0} {1} loaded.", elements.Count(), elements.First().Name);
        }

        static void PrintWithSourceLocation(this XObject x) {
            Console.WriteLine("{0}:{1}:{2}:\n{3}",
                              x.BaseUri,
                              (x as IXmlLineInfo).LineNumber,
                              (x as IXmlLineInfo).LinePosition,
                              x);
        }

        static bool CheckRequiredAttributes(this IEnumerable<XElement> elements, params string[] attrs) {
            bool ret = true;
            foreach (var attr in attrs) {
                var errs = elements.Where(x => x.Attribute(attr) == null);
                if (errs.Any()) {
                    ret = false;
                    Console.WriteLine("Error: {0} requires {1}", errs.First().Name, attr);
                    foreach (var err in errs) {
                        err.PrintWithSourceLocation();
                    }
                }
            }
            return ret;
        }

        static IEnumerable<IGrouping<string, XElement>> GetDuplicates(this IEnumerable<XElement> elements, string attr) {
            return elements.GroupBy(x => x.Attribute(attr).Value).Where(x => x.Count() != 1);
        }

        static bool CheckDuplicates(this IEnumerable<XElement> elements, params string[] attrs) {
            bool ret = true;
            foreach (var attr in attrs) {
                foreach (var errs in elements.GetDuplicates(attr)) {
                    ret = false;
                    Console.WriteLine("Error: duplicated {0}", errs.First().Attribute(attr));
                    foreach (var err in errs) {
                        err.PrintWithSourceLocation();
                    }
                }
            }
            return ret;
        }

        [STAThread]
        static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("No definitions specified. Now trying to get them from the official GitHub...");
                args = new string[] {
                    "https://raw.githubusercontent.com/mavlink/c_library_v2/master/message_definitions/all.xml",
                };
            }
            var root = Load(args.Select(x => new Uri(x)));

            var enms = root.SelectMany(x => x.Elements("mavlink").Elements("enums").Elements("enum"));
            enms.PrintSourceList();
            if (!enms.CheckRequiredAttributes("name")) return;
            foreach (var grp in enms.GetDuplicates("name")) {
                Console.WriteLine("Info: Duplicated enum {0}", grp.First().Attribute("name"));
                var conflicts = grp
                    .SelectMany(x => x.Elements("entry"))
                    .GroupBy(x => x.Elements("entry")) //!!!
                    .Where(x => x.Count() != 1);
                if (conflicts.Any()) {
                    Console.WriteLine("Error: enum entry conflicts!");
                    foreach (var x in conflicts) {
                        x.First().PrintWithSourceLocation();
                    }
                    return;
                }
                Console.WriteLine("...merged successfully");
            }

            var msgs = root.SelectMany(x => x.Elements("mavlink").Elements("messages").Elements("message"));
            msgs.PrintSourceList();
            if (!msgs.CheckRequiredAttributes("name", "id")) return;
            if (!msgs.CheckDuplicates("name", "id")) return;
        }
    }
}
