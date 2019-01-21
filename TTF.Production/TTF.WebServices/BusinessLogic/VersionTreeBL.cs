///
///<file>VersionTreeBL.cs</file>
///<description>
///VersionTreeBL is the class that implements business logic in managing versions
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>21-11-2013</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections;
using TTF.Models;
using TTF.DataLayer;
using System.Data;
using System.Data.SqlClient;

namespace TTF.BusinessLogic
{
    public class VersionTreeBL
    {
        public List<VersionTree> GetAllVersions()
        {
            return (new VersionTreeDL()).GetAllVersions();
        }

        public List<VersionTree> GetVersionsByType(string type)
        {
            return (new VersionTreeDL()).GetVersionsByType(type);
        }

        public List<VersionTree> GetFirstLevelVersionsByType(string type)
        {
            return (new VersionTreeDL()).GetFirstLevelVersionsByType(type);
        }

        public List<VersionTree> GetVersionsByName(string name)
        {
            return (new VersionTreeDL()).GetVersionsByName(name);
        }

        public List<VersionTree> GetVersionsByNameAndType(string name, string type)
        {
            return (new VersionTreeDL()).GetVersionsByNameAndType(name,type);
        }

        public VersionTree GetEffectiveVersion(string type, string date)
        {
            return (new VersionTreeDL()).GetEffectiveVersion(type, date);
        }

        public int AddVersion(int? parentID, string type, string name, string remark, short createdBy, string effectiveFrom, string effectiveTo, SqlConnection conn, SqlTransaction trans)
        {
            string version = GetNewVersionCode(parentID,type);
            return (new VersionTreeDL()).AddVersion(parentID, version, type, name, remark, createdBy,effectiveFrom,effectiveTo,conn,trans);
        }

        /// <summary>
        /// Get the new version code based on the given parent ID and version type
        /// </summary>
        /// <param name="parentID">ID of the parent version. It may be null if the new version does not have parent version</param>
        /// <param name="type">version type</param>
        /// <returns>new version code</returns>
        private string GetNewVersionCode(int? parentID, string type)
        {
            string version = "";
            VersionTreeDL dl = new VersionTreeDL();

            List<VersionTree> existingVersions = GetFirstLevelVersionsByType(type);

            if (existingVersions.Count == 0)
            {
                return "V1.0";  //default first version code
            }

            if (parentID == null) //Create a top level version
            {
                string lastVersion = existingVersions[existingVersions.Count - 1].Version;
                int lastVersionNumber = Convert.ToInt32(lastVersion.Substring(1, lastVersion.Length - 3));
                version = "V" + (lastVersionNumber + 1) + ".0";
            }
            else //Create a sub version
            {
                VersionTree parentVersion = dl.GetVersionByID(parentID.Value);
                string parentCode = parentVersion.Version;
                List<VersionTree> children = dl.GetAllChildren(parentID.Value);

                if (children.Count == 0)
                {
                    if (parentCode.Substring(parentCode.Length - 1, 1) == "0")
                    {
                        parentCode = parentCode.Substring(0, parentCode.Length - 2);
                    }
                    version = parentCode + ".1";
                }
                else
                {
                    string latestChild = children[children.Count - 1].Version;
                    string[] elements = latestChild.Split(new char[] { '.'});
                    string lastElement = elements[elements.Length - 1];
                    int versionNo = Convert.ToInt32(lastElement) + 1;
                    version = latestChild.Substring(0, latestChild.Length - lastElement.Length) + versionNo;
                }
            }

            return version;
        }

        public int UpdateVersion(int id, string name, string remark, Int16 updatedBy, string effectiveFrom, string effectiveTo)
        {
            return (new VersionTreeDL()).UpdateVersion(id, name, remark, updatedBy, effectiveFrom, effectiveTo);
        }

        public int DeleteVersionByID(int id)
        {
            return (new VersionTreeDL()).DeleteVersionByID(id);
        }

        public int DeleteVersionByType(string type)
        {
            return (new VersionTreeDL()).DeleteVersionByType(type);
        }

        public List<VersionTree> GetEffectiveRotationPlans(string date)
        {
            return new VersionTreeDL().GetEffectiveRotationPlans(date);
        }

    }
}