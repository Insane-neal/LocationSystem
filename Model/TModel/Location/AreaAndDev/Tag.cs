﻿using System.Runtime.Serialization;
using Location.IModel.Locations;
using Location.TModel.ConvertCodes;
using System;
using Location.TModel.Location.Data;
using Location.TModel.Location.Person;

namespace Location.TModel.Location.AreaAndDev
{

    /// <summary>
    /// 标签 即（定位卡）
    /// </summary>
    [ByName("LocationCard")]
    [DataContract] [Serializable]
    public class Tag: ITag
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Describe { get; set; }

        [DataMember]
        public Personnel Person { get; set; }

        [DataMember]
        public TagPosition Pos { get; set; }

        [DataMember]
        public int Power { get; set; }

        [DataMember]
        public int PowerState { get; set; }

        [DataMember]
        public string Flag { get; set; }

        [DataMember]
        public int CardRoleId { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        [DataMember]
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
