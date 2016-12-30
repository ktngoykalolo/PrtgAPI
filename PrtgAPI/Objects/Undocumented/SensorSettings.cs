﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using PrtgAPI.Enums.Deserialization;
using PrtgAPI.Helpers;

namespace PrtgAPI.Objects.Undocumented
{
    internal class SensorSettings : ObjectSettings
    {
        internal static XElement GetXml(string response, int sensorId)
        {
            var basicMatchRegex = "<input.+?name=\".*?\".+?value=\".*?\".*?>";
            var nameRegex = "(.+?name=\")(.+?)(_*\".+)"; //we might want to leave the underscores afterall

            //return GetXmlInternal(response, sensorId, basicMatchRegex, nameRegex, null);
            var inputXml = GetInputXml(response, basicMatchRegex, nameRegex, null);
            var ddlXml = GetDropDownListXml(response, nameRegex);
            var dependencyXml = GetDependency(response); //if the dependency xml is null does that cause an issue for the xelement we create below?

            var elm = new XElement("properties", inputXml, ddlXml, dependencyXml);
            return elm;
        }

        const string TimeFormat = "yyyy,MM,dd,HH,mm,ss";

        /// <summary>
        /// The name of this sensor.
        /// </summary>
        [XmlElement("injected_name")]
        public string z_Name { get; set; }

        /// <summary>
        /// Tags that are inherited from this objects parent
        /// </summary>
        [XmlElement("injected_parenttags")]
        public string z_ParentTags { get; set; }

        /// <summary>
        /// Tags that are defined on this object.
        /// </summary>
        [XmlElement("injected_tags")]
        public string z_Tags { get; set; }

        /// <summary>
        /// How raw sensor results should be stored.
        /// </summary>
        [XmlElement("injected_writeresult")]
        public DebugMode z_DebugMode { get; set; }

        /// <summary>
        /// The method used for performing WMI queries.
        /// </summary>
        [XmlElement("injected_wmialternative")]
        public WmiMode z_WmiMode { get; set; }

[XmlElement("injected_stack")]
public string Stack { get; set; }

[XmlElement("injected_stackunit")]
public string StackUnit { get; set; }

        /// <summary>
        /// Whether to inherit Scanning Interval settings from the parent object.
        /// </summary>
        [XmlElement("injected_intervalgroup")]
        public bool z_InheritScanningInterval { get; set; }

        /// <summary>
        /// Whether to inherit Schedules, Dependencies and Maintenance Window settings from the parent object.
        /// </summary>
        [XmlElement("injected_scheduledependency")]
        public bool z_InheritScheduleDependency { get; set; }

        [XmlElement("injected_maintenable")]
        public bool z_MaintenanceEnabled {get; set; }

        public DateTime z_MaintenanceStart => DateTime.ParseExact(maintenanceStart, TimeFormat, null);

        [XmlElement("injected_maintstart")]
        protected string maintenanceStart {get; set; }

        public DateTime z_MaintenanceEnd => DateTime.ParseExact(_RawMaintenanceEnd, TimeFormat, null);

        [XmlElement("injected_maintend")]
        public string _RawMaintenanceEnd { get; set; }

        [XmlElement("injected_dependencytype")]
        public DependencyType z_DependencyType {get; set; } //if you select selectobject there is a second textbox we dont have that specifies that dependency object

        private string dependencyValue;

        [XmlElement("injected_dependencyvalue")]
        public string z_DependencyValue
        {
            get { return dependencyValue; }
            set { dependencyValue = value == string.Empty ? null : value; }
        }

        /// <summary>
        /// Duration (in seconds) to delay resuming this sensor after its master object returns to <see cref="SensorStatus.Up"/>.
        /// </summary>
        [XmlElement("injected_depdelay")]
        public int z_DependencyDelay { get; set; }

        /// <summary>
        /// Whether to inherit Access Rights settings from this sensor's parent object.
        /// </summary>
        [XmlElement("injected_accessgroup")]
        public bool z_InheritAccessGroup { get; set; }

        /// <summary>
        /// Whether to inherit the Channel Unit Configuration settings from this sensor's parent object.
        /// </summary>
        [XmlElement("injected_unitconfiggroup")]
        public bool z_InheritChannelUnit { get; set; }

        /// <summary>
        /// The priority of this sensor.
        /// </summary>
        [XmlElement("injected_priority")]
        public Priority z_Priority { get; set; }

[XmlElement("injected_primarychannel")]
public string PrimaryChannel { get; set; }

        [XmlElement("injected_interval")]
        public ScanningInterval z_ScanningInterval { get; set; }  //todo: what if its a custom interval

        [XmlElement("injected_errorintervalsdown")]
        public ErrorIntervalDown z_ErrorIntervalDown { get; set; }

        [XmlElement("injected_schedule")]
        public string _RawSchedule { get; set; } //todo: remove all _raw fields in this file

        public Schedule z_Schedule => new Schedule(_RawSchedule);

        private string rawAccessRights;
        private Access accessRights;

        [XmlElement("injected_accessrights_201")]
        public string _RawAccessRights
        {
            set
            {
                rawAccessRights = value;
                accessRights = value.XmlEnumAlternateNameToEnum<Access>();
            }
            get { return rawAccessRights; }
        }

        public Access z_AccessRights => accessRights;

[XmlElement("injected_unitconfig__oukBytesMemory_volume")]
public string UnitConfig__OUKBytesMemory_Volume { get; set; }





}
}
