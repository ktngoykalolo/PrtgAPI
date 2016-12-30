﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PrtgAPI.Attributes;
using PrtgAPI.Helpers;
using PrtgAPI.Html;
using PrtgAPI.Objects.Deserialization;
using PrtgAPI.Objects.Shared;
using PrtgAPI.Objects.Undocumented;
using PrtgAPI.Parameters;

namespace PrtgAPI
{
    /// <summary>
    /// Makes API requests against a PRTG Network Monitor server.
    /// </summary>
    public partial class PrtgClient
    {
        /// <summary>
        /// Gets the PRTG server API requests will be made against.
        /// </summary>
        public string Server { get; }

        /// <summary>
        /// Gets the Username that will be used to authenticate against PRTG.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The PassHash that will be used to authenticate with, in place of a password.
        /// </summary>
        public string PassHash { get; }

        private IWebClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrtgClient"/> class.
        /// </summary>
        public PrtgClient(string server, string username, string pass, AuthMode authMode = AuthMode.Password)
            : this(server, username, pass, authMode, new WebClient())
        {
        }

        internal PrtgClient(string server, string username, string pass, AuthMode authMode, IWebClient client)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (username == null)
                throw new ArgumentNullException(nameof(username));

            if (pass == null)
                throw new ArgumentNullException(nameof(pass));

            this.client = client;

            Server = server;
            Username = username;

            PassHash = authMode == AuthMode.Password ? GetPassHash(pass) : pass;
        }

        #region Requests

        private string GetPassHash(string password)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Password] = password
            };

            var response = ExecuteRequest(JsonFunction.GetPassHash, parameters);

            return response;
        }

        private List<T> GetObjects<T>(Parameters.Parameters parameters)
        {
            var response = ExecuteRequest(XmlFunction.TableData, parameters);

#if DEBUG
            //Debug.WriteLine(response.ToString());
#endif

            //todo: change the Property enum to have a description attribute that lets you change the property name to something else
            //then, change ObjId to just Id

            //todo - check the xml doesnt say there was an error
            //it looks like we already do this when its an xml request, however theres also a bug here in that we automatically try to deserialize
            //some xml without checking whether its xml or not; this could result in an exception in the exception handler!
            //we need to be able to handle errors on json requests or otherwise
            //it looks like these properties are ultimately parsed by prtgurl, so we'd need to change prtgurl to try and get the description property
            //if it detects a parameters object type is an enum

            //todo: upload our empty settings file then say dont track it
            //git update-index --assume-unchanged <file> and --no-assume-unchanged

            //redundant: deserializelist already does this exception handling

            var data = Data<T>.DeserializeList(response);

            return data.Items;
        }

        /// <summary>
        /// Calcualte the total number of objects of a given type present on a PRTG Server.
        /// </summary>
        /// <param name="content">The type of object to total.</param>
        /// <returns>The total number of objects of a given type.</returns>
        public int GetTotalObjects(Content content)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Count] = 0,
                [Parameter.Content] = content
            };

            return Convert.ToInt32(GetObjectsRaw<PrtgObject>(parameters).TotalCount);
        }

        #region Sensors

        /// <summary>
        /// Retrieve all sensors from a PRTG Server.
        /// </summary>
        /// <returns>A list of all sensors on a PRTG Server.</returns>
        public List<Sensor> GetSensors()
        {
            return GetObjects<Sensor>(new SensorParameters());
        }

        #region SensorStatus
        
        /// <summary>
        /// Retrieve sensors from a PRTG Server of one or more statuses.
        /// </summary>
        /// <param name="sensorStatuses">A list of sensor statuses to filter for.</param>
        /// <returns></returns>
        public List<Sensor> GetSensors(params SensorStatus[] sensorStatuses)
        {
            return GetSensors(new SensorParameters { StatusFilter = sensorStatuses });
        }

        /// <summary>
        /// Retrieve the number of sensors of each sensor type in the system.
        /// </summary>
        /// <returns></returns>
        public SensorTotals GetSensorTotals()
        {
            var response = ExecuteRequest(XmlFunction.GetTreeNodeStats, new Parameters.Parameters());

            return Data<SensorTotals>.DeserializeType(response);
        }

        #endregion 

        #region SearchFilter

        /// <summary>
        /// Retrieve sensors from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Sensor> GetSensors(Property property, object value)
        {
            return GetSensors(new SearchFilter(property, value));
        }

        /// <summary>
        /// Retrieve sensors from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="operator">Operator to compare value and property value with.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Sensor> GetSensors(Property property, FilterOperator @operator, object value)
        {
            return GetSensors(new SearchFilter(property, @operator, value));
        }

        /// <summary>
        /// Retrieve sensors from a PRTG Server based on the values of multiple properties.
        /// </summary>
        /// <param name="filters">One or more filters used to limit search results.</param>
        /// <returns></returns>
        public List<Sensor> GetSensors(params SearchFilter[] filters)
        {
            return GetSensors(new SensorParameters { SearchFilter = filters });
        }

        #endregion

        /// <summary>
        /// Retrieve sensors from a PRTG Server using a custom set of parameters.
        /// </summary>
        /// <param name="parameters">A custom set of parameters used to retrieve PRTG Sensors.</param>
        /// <returns>A list of sensors that match the specified parameters.</returns>
        public List<Sensor> GetSensors(SensorParameters parameters)
        {
            return GetObjects<Sensor>(parameters);
        }

        #endregion

        #region Devices

        /// <summary>
        /// Retrieve all devices from a PRTG Server.
        /// </summary>
        /// <returns></returns>
        public List<Device> GetDevices()
        {
            return GetObjects<Device>(new DeviceParameters());
        }

        /// <summary>
        /// Retrieve devices from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Device> GetDevices(Property property, object value)
        {
            return GetDevices(new SearchFilter(property, value));
        }

        /// <summary>
        /// Retrieve devices from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="operator">Operator to compare value and property value with.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Device> GetDevices(Property property, FilterOperator @operator, string value)
        {
            return GetDevices(new SearchFilter(property, @operator, value));
        }

        /// <summary>
        /// Retrieve devices from a PRTG Server based on the values of multiple properties.
        /// </summary>
        /// <param name="filters">One or more filters used to limit search results.</param>
        /// <returns></returns>
        public List<Device> GetDevices(params SearchFilter[] filters)
        {
            return GetDevices(new DeviceParameters { SearchFilter = filters });
        }

        /// <summary>
        /// Retrieve devices from a PRTG Server using a custom set of parameters.
        /// </summary>
        /// <param name="parameters">A custom set of parameters used to retrieve PRTG Devices.</param>
        /// <returns></returns>
        public List<Device> GetDevices(DeviceParameters parameters)
        {
            return GetObjects<Device>(parameters);
        }

        #endregion

        #region Groups

        /// <summary>
        /// Retrieve all groups from a PRTG Server.
        /// </summary>
        /// <returns></returns>
        public List<Group> GetGroups()
        {
            return GetGroups(new GroupParameters());
        }

        /// <summary>
        /// Retrieve groups from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Group> GetGroups(Property property, object value)
        {
            return GetGroups(new SearchFilter(property, value));
        }

        /// <summary>
        /// Retrieve groups from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="operator">Operator to compare value and property value with.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Group> GetGroups(Property property, FilterOperator @operator, string value)
        {
            return GetGroups(new SearchFilter(property, @operator, value));
        }

        /// <summary>
        /// Retrieve groups from a PRTG Server based on the values of multiple properties.
        /// </summary>
        /// <param name="filters">One or more filters used to limit search results.</param>
        /// <returns></returns>
        public List<Group> GetGroups(params SearchFilter[] filters)
        {
            return GetGroups(new GroupParameters { SearchFilter = filters });
        }

        /// <summary>
        /// Retrieve groups from a PRTG Server using a custom set of parameters.
        /// </summary>
        /// <param name="parameters">A custom set of parameters used to retrieve PRTG Groups.</param>
        public List<Group> GetGroups(GroupParameters parameters)
        {
            return GetObjects<Group>(parameters);
        }

        #endregion

        #region Probes

        /// <summary>
        /// Retrieve all probes from a PRTG Server.
        /// </summary>
        /// <returns></returns>
        public List<Probe> GetProbes()
        {
            return GetObjects<Probe>(new ProbeParameters());
        }

        /// <summary>
        /// Retrieve probes from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Probe> GetProbes(Property property, object value)
        {
            return GetProbes(new SearchFilter(property, value));
        }

        /// <summary>
        /// Retrieve probes from a PRTG Server based on the value of a certain property.
        /// </summary>
        /// <param name="property">Property to search against.</param>
        /// <param name="operator">Operator to compare value and property value with.</param>
        /// <param name="value">Value to search for.</param>
        /// <returns></returns>
        public List<Probe> GetProbes(Property property, FilterOperator @operator, string value)
        {
            return GetProbes(new SearchFilter(property, @operator, value));
        }

        /// <summary>
        /// Retrieve probes from a PRTG Server based on the values of multiple properties.
        /// </summary>
        /// <param name="filters">One or more filters used to limit search results.</param>
        /// <returns></returns>
        public List<Probe> GetProbes(params SearchFilter[] filters)
        {
            return GetObjects<Probe>(new ProbeParameters {SearchFilter = filters});
        }

        /// <summary>
        /// Retrieve probes from a PRTG Server using a custom set of parameters.
        /// </summary>
        /// <param name="parameters">A custom set of parameters used to retrieve PRTG Probes.</param>
        public List<Probe> GetProbes(ProbeParameters parameters)
        {
            return GetObjects<Probe>(parameters);
        }

        #endregion

        #region Channels

        /// <summary>
        /// Retrieve all channels of a sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor to retrieve channels for.</param>
        /// <returns></returns>
        public List<Channel> GetChannels(int sensorId)
        {
            var response = ExecuteRequest(XmlFunction.TableData, new ChannelParameters(sensorId));

            var items = response.Descendants("item").ToList();

            foreach (var item in items)
            {
                var id = Convert.ToInt32(item.Element("objid").Value);

                var properties = GetChannelProperties(sensorId, id);

                item.Add(properties.Nodes());
                item.Add(new XElement("injected_sensorId", sensorId));
            }

            return Data<Channel>.DeserializeList(response).Items;
        }

        internal XElement GetChannelProperties(int sensorId, int channelId)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = sensorId,
                [Parameter.Channel] = channelId
            };

            var response = ExecuteRequest(HtmlFunction.ChannelEdit, parameters);

            return ChannelSettings.GetXml(response, channelId);
        }

        internal SensorSettings GetSensorSettings(int sensorId)
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = sensorId,
                [Parameter.ObjectType] = BaseType.Sensor
            };

            //we'll need to add support for dropdown lists too

            var response = ExecuteRequest(HtmlFunction.ObjectData, parameters);

            var blah = SensorSettings.GetXml(response, sensorId);

            var doc = new XDocument(blah);

            var aaaa = Data<SensorSettings>.DeserializeType(doc);

            //maybe instead of having an enum for my schedule and scanninginterval we have a class with a special getter that removes the <num>|component when you try and retrieve the property
            //the thing is, the enum IS actually dynamic - we need a list of valid options

            //todo: whenever we use _raw attributes dont we need to add xmlignore on the one that accesses it/

            return aaaa;
        }

        public void blah()
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Custom] = new CustomParameter("limitmaxerror_0", ""),
                //prtgurl needs to throw an exception if you dont use a customparameter with parameter.custom and detect if it contains a list
                [Parameter.Id] = 2196
            };

            var response = ExecuteRequest(HtmlFunction.EditSettings, parameters);
        }



        #endregion

        #region Notification Actions       

        //todo: move this
        private List<SensorHistory> GetSensorHistory(int sensorId)
        {
            Logger.DebugEnabled = false;

            //todo: add xml formatting
            //var awwww = typeof (SensorOrDeviceOrGroupOrProbe).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            //var q = GetSensors(SensorStatus.Paused);

            //var filter = new SearchFilter(Property.Comments, SensorStatus.Paused);
            //filter.ToString();

            //myenum val = myenum.Val5;

            //var al= Enum.GetValues(typeof (myenum)).Cast<myenum>().Where(m => m != val && val.HasFlag(m)).ToList();

            //var enums = Enum.GetValues(typeof (myenum)).Cast<myenum>().ToList();


            //var result = myenum.Val5.GetUnderlyingFlags();

            //var result = outer(myenum.Val5, enums).ToList();

            //foreach (var a in al1)
            

            //loop over each element. if a value contains more than 1 element in it, its not real, so ignore it

            var parameters = new Parameters.Parameters
            {
                [Parameter.Columns] = new[] {Property.Datetime, Property.Value_, Property.Coverage},
                [Parameter.Id] = 2196,
                [Parameter.Content] = Content.Values
            };

            var items = GetObjects<SensorHistoryData>(parameters);

            foreach (var history in items)
            {
                foreach (var value in history.Values)
                {
                    value.DateTime = history.DateTime;
                    value.SensorId = sensorId;
                }
            }
            //todo: need to implement coverage column
            //todo: right now the count is just the default - 500. need to allow specifying bigger counts
            return items.SelectMany(i => i.Values).OrderByDescending(a => a.DateTime).Where(v => v.Value != null).ToList();
        }

        /// <summary>
        /// Retrieve all notification triggers of a PRTG Object.
        /// </summary>
        /// <param name="objectId">The object to retrieve triggers for.</param>
        /// <returns>A list of notification triggers that apply to the specified object.</returns>
        public List<NotificationTrigger> GetNotificationTriggers(int objectId)
        {
            //var allRaw = GetNotificationTriggers(objectId, Content.Triggers);
            //var all = GetNotificationTriggers(allRaw, objectId);
            //return all;

            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = objectId,
                [Parameter.Content] = Content.Triggers,
                [Parameter.Columns] = new [] { Property.Content, Property.ObjId }
            };

            var xmlResponse = ExecuteRequest(XmlFunction.TableData, parameters);
            var xmlResponseContent = xmlResponse.Descendants("item").Select(x => new
            {
                Content = x.Element("content").Value,
                Id = Convert.ToInt32(x.Element("objid").Value)
            }).ToList();
            //var xmlResponseContent = xmlResponse.Descendants("content").Select(x => x.Value).ToList(); //we have an objid now so we need to handle that!
            var triggers = JsonDeserializer<NotificationTrigger>.DeserializeList(xmlResponseContent, e => e.Content,
                (e, o) =>
                {
                    o.SubId = e.Id;
                    o.ObjectId = objectId;
                });

            return triggers;

            //var jsonResponse = ExecuteRequest(JsonFunction.Triggers, parameters);
            //var jsonData = JsonDeserializer<NotificationTriggerData>.DeserializeType(jsonResponse);
            
            //objectlinkxml contains the parentid, whereas triggers.json contains the subid

            //foreach (var trigger in triggers)
            //{
            //    trigger.ObjectId = objectId;

                //we need to loop between the jsonresponse to add the subid to the xml response, however how am i supposed to
                //identify which subid belongs to which trigger when the subid is what unique identifies a trigger!
            //}

            throw new NotImplementedException();

            //return data.Triggers.ToList();







            //if thisid != parentid, its inherited



            //var inheritedRaw = GetNotificationTriggers(objectId, Content.Trigger).ToList();


            //var nonInheritedRaw = allRaw.Except(inheritedRaw);






            //var nonInherited = GetNotificationTriggers(nonInheritedRaw, objectId);



            //inherited triggers - https://prtg.example.com/api/table.xml?id=49229&content=triggers&columns=content
            //then, we have to do a diff






            //http://prtg.example.com/api/table.json?content=triggers&id=1&columns=content



            //using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(response)))
            {
            //    var data = (NotificationTriggerData) deserializer.ReadObject(stream);

            //    return data.Triggers.ToList();
            }






            //http://stackoverflow.com/questions/814001/how-to-convert-json-to-xml-or-xml-to-json



            //todo: need to handle no content

            //editsettings?inherittriggers_=0&id=1


            //configure sensor 2196 to have a go green event and compare the json

            //how do we enable/disable inheritance of notifications
            //POST /editsettings?nodest_new=0&latency_new=60&onnotificationid_new=300%7CEmail+and+push+notification+to+admin%7C&esclatency_new=300&escnotificationid_new=-1%7CNone%7C&repeatival_new=0&offnotificationid_new=-1%7CNone%7C&subid=new&objecttype=nodetrigger&class=state&ajaxrequest=1&id=2196 HTTP/1.1
            //how do we get the setting of whether inheritance is enabled/disabled
            //http://prtg.example.com/controls/triggersandnotifications.htm?id=2196
            //how do we get the values of the inherited notifications

            //i bet theres a way we can get ALL the info in a single request

            //there is a log.htm page that takes ?filter_status filters.

            //apparently its valid to set * as a valid for parameter count

            //http://prtg.example.com/api/triggers.json?id=2196&subid=1

            //all triggers:

            //http://prtg.example.com/api/triggers.json?id=1

            //have our get-notification cmdlet called gettriggers. maybe rename this function too?

            //todo: add sensorid to the table display for get-channelproperty
        }

        /// <summary>
        /// Retrieve all notification actions on a PRTG Server.
        /// </summary>
        /// <returns></returns>
        public List<NotificationAction> GetNotificationActions()
        {
            return GetObjects<NotificationAction>(new NotificationActionParameters());
        }

        /// <summary>
        /// Add or edit a notification trigger on a PRTG Server.
        /// </summary>
        /// <param name="parameters">A set of parameters describing the type of notification trigger and how to manipulate it.</param>
        public void SetNotificationTrigger(TriggerParameters parameters)
        {
            ExecuteRequest(HtmlFunction.EditSettings, parameters);
            //var p = new StateTriggerParameters(2196, null, ModifyAction.Add, TriggerSensorState.Down); //will the fact we tolower our value prevent the notificationaction from working?
                                                                                                       //p.Latency = 30;

            //how do you delete a trigger?
                //GET /deletesub.htm?id=2196&subid=7&_=1481973979539 HTTP/1.1
            //we can have a new-triggerparameter that takes a -type and gives back the appropriate object

            //var url = new PrtgUrl(Server, Username, PassHash, HtmlFunction.EditSettings, p);

            /*
            
            nodest_new=0&
            latency_new=60&
            onnotificationid_new=300%7CEmail+and+push+notification+to+admin%7C&
            esclatency_new=300&
            escnotificationid_new=-1%7CNone%7C&
            offnotificationid_new=-1%7CNone%7C&
            repeatival_new=0&
            <optional offnotificationid>



            subid=new&
            objecttype=nodetrigger&
            class=state&
            ajaxrequest=1&
            id=2196
            */

            //POST /editsettings? HTTP/1.1

            //some have _new, some dont. what are the fields called when you EDIT a trigger?

            //POST /editsettings?nodest_4=0&latency_4=70&onnotificationid_4=300%7CEmail+and+push+notification+to+admin%7C&esclatency_4=300&escnotificationid_4=301%7CEmail+to+all+members+of+group+PRTG+Users+Group%7C&repeatival_4=3&offnotificationid_4=302%7CTicket+Notification%7C&id=1&subid=4 HTTP/1.1

            /*
            //POST /editsettings?
            
            nodest_4=0&
            latency_4=70&
            onnotificationid_4=300%7CEmail+and+push+notification+to+admin%7C&
            esclatency_4=300
            &escnotificationid_4=301%7CEmail+to+all+members+of+group+PRTG+Users+Group%7C
            &repeatival_4=3&
            offnotificationid_4=302%7CTicket+Notification%7C&
            id=1&subid=4 HTTP/1.1
            */



            //instead of appending "new", append the subid of the trigger

        }

        public void RemoveNotificationTrigger(int objectId, int triggerId)
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = objectId,
                [Parameter.SubId] = triggerId
            };

            ExecuteRequest(HtmlFunction.RemoveSubObject, parameters);
        }

        #endregion

        #region Pause / Resume

        /// <summary>
        /// Mark a <see cref="SensorStatus.Down"/> sensor as <see cref="SensorStatus.DownAcknowledged"/>. If an acknowledged sensor returns to <see cref="SensorStatus.Up"/>, it will not be acknowledged when it goes down again.
        /// </summary>
        /// <param name="objectId">ID of the sensor to acknowledge.</param>
        /// <param name="message">Message to display on the acknowledged sensor.</param>
        /// <param name="duration">Duration (in minutes) to acknowledge the object for. If null, sensor will be paused indefinitely.</param>
        public void AcknowledgeSensor(int objectId, string message = null, int? duration = null)
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = objectId,
                //[Parameter.AcknowledgeMessage] = message,
            };

            if (message != null)
                parameters[Parameter.AcknowledgeMessage] = message;

            if (duration != null)
                parameters[Parameter.Duration] = duration;

            ExecuteRequest(CommandFunction.AcknowledgeAlarm, parameters);
        }

        /// <summary>
        /// Pause a PRTG Object (sensor, device, etc).
        /// </summary>
        /// <param name="objectId">ID of the object to pause.</param>
        /// <param name="pauseMessage">Message to display on the paused object.</param>
        /// <param name="durationMinutes">Duration (in minutes) to pause the object for. If null, object will be paused indefinitely.</param>
        public void Pause(int objectId, string pauseMessage = null, int? durationMinutes = null)
        {
            PauseParametersBase parameters;

            if (durationMinutes == null)
            {
                parameters = new PauseParameters(objectId);
            }
            else
            {
                parameters = new PauseForDurationParameters(objectId, (int)durationMinutes);
            }

            if (pauseMessage != null)
                parameters.PauseMessage = pauseMessage;

            ExecuteRequest(CommandFunction.Pause, parameters);
        }

        /// <summary>
        /// Resume a PRTG Object (e.g. sensor or device) from a Paused or Simulated Error state.
        /// </summary>
        /// <param name="objectId">ID of the object to resume.</param>
        public void Resume(int objectId)
        {
            var parameters = new PauseParameters(objectId, PauseAction.Resume);

            ExecuteRequest(CommandFunction.Pause, parameters);
        }

        /// <summary>
        /// Simulate an error state for a sensor.
        /// </summary>
        /// <param name="sensorId">ID of the sensor to simulate an error for.</param>
        public void SimulateError(int sensorId)
        {
            ExecuteRequest(CommandFunction.Simulate, new SimulateErrorParameters(sensorId));
        }

        #endregion

        #region ExecuteRequest

        private string ExecuteRequest(JsonFunction function, Parameters.Parameters parameters)
        {
            var url = new PrtgUrl(Server, Username, PassHash, function, parameters);

            var response = ExecuteRequest(url);

            return response;
        }

        private XDocument ExecuteRequest(XmlFunction function, Parameters.Parameters parameters)
        {
            var url = new PrtgUrl(Server, Username, PassHash, function, parameters);

            var response = ExecuteRequest(url);

            return XDocument.Parse(XDocumentHelpers.SanitizeXml(response));
        }

        private void ExecuteRequest(CommandFunction function, Parameters.Parameters parameters)
        {
            var url = new PrtgUrl(Server, Username, PassHash, function, parameters);

            var response = ExecuteRequest(url);
        }

        private string ExecuteRequest(HtmlFunction function, Parameters.Parameters parameters)
        {
            var url = new PrtgUrl(Server, Username, PassHash, function, parameters);

            var response = ExecuteRequest(url);

            return response;
        }

        private string ExecuteRequest(PrtgUrl url)
        {
            string response;

            try
            {
                //var client = new WebClient();
                response = client.DownloadString(url.Url);
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw;

                var webResponse = (HttpWebResponse)ex.Response;

                if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        response = reader.ReadToEnd();
                        
                        var xDoc = XDocument.Parse(response);
                        var errorMessage = xDoc.Descendants("error").First().Value;

                        throw new PrtgRequestException($"PRTG was unable to complete the request. The server responded with the following error: {errorMessage}", ex);
                    }
                }

                throw;
            }

            return response;
        }

        #endregion

        #region SetObjectProperty

        #region BasicObjectSetting

        /// <summary>
        /// Modify basic object settings (name, tags, priority, etc.) for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to modify.</param>
        /// <param name="name">The setting to whose value will be overwritten.</param>
        /// <param name="value">Value of the setting to apply.</param>
        public void SetObjectProperty(int objectId, BasicObjectSetting name, string value)
        {
            SetObjectProperty(new SetObjectSettingParameters<BasicObjectSetting>(objectId, name, value));
        }

        #endregion

        #region ScanningInterval

        /// <summary>
        /// Modify scanning interval settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to modify.</param>
        /// <param name="name">The setting to whose value will be overwritten.</param>
        /// <param name="value">Value of the setting to apply.</param>
        private void SetObjectProperty(int objectId, ScanningInterval name, object value)
        {
            SetObjectProperty(new SetObjectSettingParameters<ScanningInterval>(objectId, name, value));
        }

        #endregion

        #region SensorDisplay

        /// <summary>
        /// Modify sensor display settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to modify.</param>
        /// <param name="name">The setting to whose value will be overwritten.</param>
        /// <param name="value">Value of the setting to apply.</param>
        private void SetObjectProperty(int objectId, SensorDisplay name, object value)
        {
            SetObjectProperty(new SetObjectSettingParameters<SensorDisplay>(objectId, name, value));
        }

        #endregion

        #region ExeScriptSetting

        /// <summary>
        /// Modify EXE/Script settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to modify.</param>
        /// <param name="name">The setting to whose value will be overwritten.</param>
        /// <param name="value">Value of the setting to apply.</param>
        private void SetObjectProperty(int objectId, ExeScriptSetting name, object value)
        {
            SetObjectProperty(new SetObjectSettingParameters<ExeScriptSetting>(objectId, name, value));
        }

        #endregion

        #region Channel

        /// <summary>
        /// Modify channel properties for a PRTG Sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor whose channels should be modified.</param>
        /// <param name="channelId">The ID of the channel to modify.</param>
        /// <param name="property">The property of the channel to modify</param>
        /// <param name="value">The value to set the channel's property to.</param>
        public void SetObjectProperty(int sensorId, int channelId, ChannelProperty property, object value)
        {
            var customParams = GetChannelSetObjectPropertyCustomParams(channelId, property, value);

            var parameters = new Parameters.Parameters
            {
                [Parameter.Custom] = customParams,
                [Parameter.Id] = sensorId
            };

            ExecuteRequest(HtmlFunction.EditSettings, parameters);
        }

        private List<CustomParameter> GetChannelSetObjectPropertyCustomParams(int channelId, ChannelProperty property, object value)
        {
            bool valAsBool;
            var valIsBool = bool.TryParse(value.ToString(), out valAsBool);

            List<CustomParameter> customParams = new List<CustomParameter>();

            if (valIsBool)
            {
                if (valAsBool)
                {
                    value = 1;
                }

                else //if we're disabling a property, check if there are values dependent on us. if so, disable them too!
                {
                    value = 0;

                    var associatedProperties = property.GetDependentProperties<ChannelProperty>();

                    customParams.AddRange(associatedProperties.Select(prop => Channel.CreateCustomParameter(prop, channelId, string.Empty)));
                }
            }
            else //if we're enabling a property, check if there are values we depend on. if so, enable them!
            {
                var dependentProperty = property.GetEnumAttribute<DependentPropertyAttribute>();

                if (dependentProperty != null)
                {
                    customParams.Add(Channel.CreateCustomParameter(dependentProperty.Name.ToEnum<ChannelProperty>(), channelId, "1"));
                }
            }

            customParams.Add(Channel.CreateCustomParameter(property, channelId, value));

            return customParams;
        }

        #endregion

        private void SetObjectProperty<T>(SetObjectSettingParameters<T> parameters)
        {
            ExecuteRequest(CommandFunction.SetObjectProperty, parameters);
        }

        #endregion

        #region GetObjectProperty

        #region BasicObjectSetting

        /// <summary>
        /// Retrieves basic object settings (name, tags, priority, etc.) for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        public string GetObjectProperty(int objectId, BasicObjectSetting name)
        {
            return GetObjectProperty<string>(objectId, name);
        }

        /// <summary>
        /// Retrieves basic object settings (name, tags, priority, etc.) in their true data type for a PRTG Object.
        /// </summary>
        /// <typeparam name="T">The return type suggested by the documentation for the <see cref="BasicObjectSetting"/> specified in <paramref name="name"/>.</typeparam>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        public T GetObjectProperty<T>(int objectId, BasicObjectSetting name)
        {
            return GetObjectProperty<T, BasicObjectSetting>(new GetObjectSettingParameters<BasicObjectSetting>(objectId, name));
        }

        #endregion

        #region ScanningInterval

        /// <summary>
        /// Retrieves scanning interval related settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private string GetObjectProperty(int objectId, ScanningInterval name)
        {
            return GetObjectProperty<string, ScanningInterval>(new GetObjectSettingParameters<ScanningInterval>(objectId, name));
        }

        /// <summary>
        /// Retrieves scanning interval related settings in their true data type for a PRTG Object.
        /// </summary>
        /// <typeparam name="T">The return type suggested by the documentation for the <see cref="ScanningInterval"/> specified in <paramref name="name"/>.</typeparam>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private T GetObjectProperty<T>(int objectId, ScanningInterval name)
        {
            return GetObjectProperty<T, ScanningInterval>(new GetObjectSettingParameters<ScanningInterval>(objectId, name));
        }

        #endregion

        #region SensorDisplay

        /// <summary>
        /// Retrieves sensor display settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private string GetObjectProperty(int objectId, SensorDisplay name)
        {
            return GetObjectProperty<string, SensorDisplay>(new GetObjectSettingParameters<SensorDisplay>(objectId, name));
        }

        /// <summary>
        /// Retrieves sensor display settings in their true data type for a PRTG Object.
        /// </summary>
        /// <typeparam name="T">The return type suggested by the documentation for the <see cref="SensorDisplay"/> specified in <paramref name="name"/>.</typeparam>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private T GetObjectProperty<T>(int objectId, SensorDisplay name)
        {
            return GetObjectProperty<T, SensorDisplay>(new GetObjectSettingParameters<SensorDisplay>(objectId, name));
        }

        #endregion

        #region ExeScriptSetting

        /// <summary>
        /// Retrieves EXE/Script settings for a PRTG Object.
        /// </summary>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private string GetObjectProperty(int objectId, ExeScriptSetting name)
        {
            return GetObjectProperty<string, ExeScriptSetting>(new GetObjectSettingParameters<ExeScriptSetting>(objectId, name));
        }

        /// <summary>
        /// Retrieves EXE/Script settings in their true data type for a PRTG Object.
        /// </summary>
        /// <typeparam name="T">The return type suggested by the documentation for the <see cref="ExeScriptSetting"/> specified in <paramref name="name"/>.</typeparam>
        /// <param name="objectId">ID of the object to retrieve settings for.</param>
        /// <param name="name">The setting to retrieve.</param>
        /// <returns>The value of the requested setting.</returns>
        private T GetObjectProperty<T>(int objectId, ExeScriptSetting name)
        {
            return GetObjectProperty<T, ExeScriptSetting>(new GetObjectSettingParameters<ExeScriptSetting>(objectId, name));
        }

        #endregion
        
        private TReturn GetObjectProperty<TReturn, TEnum>(GetObjectSettingParameters<TEnum> parameters)
        {
            var response = ExecuteRequest(XmlFunction.GetObjectProperty, parameters);

            var value = response.Descendants("result").First().Value;

            if (value == "(Property not found)")
                throw new PrtgRequestException("PRTG was unable to complete the request. A value for property '" + parameters.Name + "' could not be found.");

            if (typeof(TReturn).IsEnum)
            {
                return (TReturn)(object)Convert.ToInt32(value);
            }
            if (typeof (TReturn) == typeof (int))
            {
                return (TReturn)(object)Convert.ToInt32(value);
            }
            if (typeof (TReturn) == typeof (string))
            {
                return (TReturn) (object)value;
            }
            throw new UnknownTypeException(typeof(TReturn));
        }

        #endregion

        #endregion

        #region Miscellaneous

        //todo: check all arguments we can in this file and make sure we validate input. when theres a chain of methods, validate on the inner most one except if we pass a parameter object, in which case validate both

        /// <summary>
        /// Request an object or any children of an object refresh themselves immediately.
        /// </summary>
        /// <param name="objectId">The ID of the sensor, or the ID of a Probe, Group or Device whose child sensors should be refreshed.</param>
        public void CheckNow(int objectId)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = objectId
            };

            ExecuteRequest(CommandFunction.ScanNow, parameters);
        }

        /// <summary>
        /// Automatically create sensors under an object based on the object's (or it's children's) device type.
        /// </summary>
        /// <param name="objectId">The object to run Auto-Discovery for (such as a device or group).</param>
        public void AutoDiscover(int objectId)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = objectId
            };
            
            ExecuteRequest(CommandFunction.DiscoverNow, parameters);
        }

        /// <summary>
        /// Modify the position of an object up or down within the PRTG User Interface.
        /// </summary>
        /// <param name="objectId">The object to reposition.</param>
        /// <param name="position">The direction to move in.</param>
        public void SetPosition(int objectId, Position position)
        {
            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = objectId,
                [Parameter.NewPos] = position
            };

            ExecuteRequest(CommandFunction.SetPosition, parameters);
        }

        /// <summary>
        /// Clone a sensor or group to another device or group respectively.
        /// </summary>
        /// <param name="sourceObjectId">The ID of a sensor or group to clone.</param>
        /// <param name="cloneName">The name that should be given to the cloned object.</param>
        /// <param name="targetLocationObjectId">If this is a sensor, the ID of the device to clone to. If this is a group, the ID of the group to clone to.</param>
        public void Clone(int sourceObjectId, string cloneName, int targetLocationObjectId)
        {
            if (cloneName == null)
                throw new ArgumentNullException(nameof(cloneName));

            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = sourceObjectId,
                [Parameter.Name] = cloneName,
                [Parameter.TargetId] = targetLocationObjectId
            };

            //todo: need to implement simulateerrorparameters or get rid of it?

            ExecuteRequest(CommandFunction.DuplicateObject, parameters);

            //todo: apparently the server replies with the url of the new page, which we could parse into an object containing the id of the new object and return from this method

            //get-sensor|copy-object -target $devices
        }

        /// <summary>
        /// Clone a device to another group.
        /// </summary>
        /// <param name="deviceId">The ID of the device to clone.</param>
        /// <param name="cloneName">The name that should be given to the cloned device.</param>
        /// <param name="host">The hostname or IP Address that should be assigned to the new device.</param>
        /// <param name="targetGroupId">The group the device should be cloned to.</param>
        public void Clone(int deviceId, string cloneName, string host, int targetGroupId)
        {
            if (cloneName == null)
                throw new ArgumentNullException(nameof(cloneName));

            if (host == null)
                throw new ArgumentNullException(nameof(host));

            var parameters = new Parameters.Parameters()
            {
                [Parameter.Id] = deviceId,
                [Parameter.Name] = cloneName,
                [Parameter.Host] = host,
                [Parameter.TargetId] = targetGroupId
            };

            //todo: apparently the server replies with the url of the new page, which we could parse into an object containing the id of the new object and return from this method
        }

        /// <summary>
        /// Permanently delete an object from PRTG. This cannot be undone.
        /// </summary>
        /// <param name="id">ID of the object to delete.</param>
        public void Delete(int id)
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = id,
                [Parameter.Approve] = 1
            };

            ExecuteRequest(CommandFunction.DeleteObject, parameters);
        }

        /// <summary>
        /// Rename an object.
        /// </summary>
        /// <param name="objectId">ID of the object to rename.</param>
        /// <param name="name">New name to give the object.</param>
        public void Rename(int objectId, string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = objectId,
                [Parameter.Value] = name
            };

            ExecuteRequest(CommandFunction.Rename, parameters);
        }

        internal ServerStatus GetStatus()
        {
            var parameters = new Parameters.Parameters
            {
                [Parameter.Id] = 0
            };

            var response = ExecuteRequest(XmlFunction.GetStatus, parameters);

            return Data<ServerStatus>.DeserializeType(response);
        }

        #endregion
    }
}
