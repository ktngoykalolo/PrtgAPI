﻿using System.Collections;
using PrtgAPI.Request;

namespace PrtgAPI.Parameters
{
    /// <summary>
    /// Allows inserting custom parameters not supported by PrtgAPI.
    /// </summary>
    public class CustomParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets how the <see cref="Value"/> should be formatted when it contains a value that is <see cref="IEnumerable"/>.
        /// </summary>
        public ParameterType ParameterType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomParameter"/> class.
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="value">The value of the parameter. The caller is responsible for ensuring the value and type contains the correct capitalization and is formatted corectly when converted <see cref="ToString"/>.</param>
        public CustomParameter(string name, object value)
        {
            Name = name;
            Value = value;
            ParameterType = ParameterType.SingleValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomParameter"/> class with a specified parameter type.
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="value">The value of the parameter. The caller is responsible for ensuring the value and type contains the correct capitalization and is formatted corectly when converted <see cref="ToString"/>.</param>
        /// <param name="parameterType">How the <paramref name="value"/> should be formatted if it contains a string.</param>
        public CustomParameter(string name, object value, ParameterType parameterType)
        {
            Name = name;
            Value = value;
            ParameterType = parameterType;
        }

        /// <summary>
        /// Returns a string representation of the current object based on how it will be likely formatted when used in a <see cref="PrtgUrl"/>.
        /// </summary>
        /// <returns>The formatted representation of this parameter.</returns>
        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
