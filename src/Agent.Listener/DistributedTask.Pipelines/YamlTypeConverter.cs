﻿using System;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines
{
    internal sealed class PipelineIteratorValueYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(IteratorValueTemplate).IsAssignableFrom(type);
            //return false;
            // return type == typeof(string) ||
            //     type == typeof(List<String>) ||
            //     type == typeof(List<Dictionary<String, String>>);
        }

        public object ReadYaml(
            IParser parser,
            Type type)
        {
            var stringToken = parser.Allow<Scalar>();
            if (stringToken == null)
            {
                var arrayToken = parser.Allow<SequenceStart>();
                if (arrayToken == null)
                {
                    throw new SyntaxErrorException(parser.Current.Start, parser.Current.End, "Expected a string, string array, or mapping");
                }

                // first determine the mode we are reading in
                var simpleArray = parser.Accept<Scalar>();
                var mappingArray = parser.Accept<MappingStart>();

                List<String> simpleValues = new List<string>();
                List<Dictionary<String, String>> mappingValues = new List<Dictionary<string, string>>();
                while (parser.Peek<SequenceEnd>() == null)
                {
                    if (simpleArray)
                    {
                        simpleValues.Add(parser.Expect<Scalar>().Value);
                    }
                    else
                    {
                        mappingValues.Add(parser.ReadMappingOfStringString());
                    }
                }

                parser.Expect<SequenceEnd>();

                return simpleArray ? new IteratorValueTemplate(simpleValues) : new IteratorValueTemplate(mappingValues);
            }
            else
            {
                return new IteratorValueTemplate(stringToken.Value);
            }
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class PipelineStepYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(PipelineJobStep).IsAssignableFrom(type);
            // return type == typeof(ImportStep) ||
            //     type == typeof(ExportStep) ||
            //     type == typeof(GroupStep) ||
            //     type == typeof(TaskStep);
        }

        public Object ReadYaml(
            IParser parser,
            Type type)
        {
            PipelineJobStep step = null;

            parser.Expect<MappingStart>();

            var stepType = parser.Expect<Scalar>();
            if (stepType.Value.Equals("import"))
            {
                var resource = parser.Expect<Scalar>();
                step = new ImportStep { Name = resource.Value };
            }
            else if (stepType.Value.Equals("group"))
            {
                var groupName = parser.Expect<Scalar>();
                step = new GroupStep { Name = groupName.Value };
            }
            else if (stepType.Value.Equals("task"))
            {
                var taskRefString = parser.Expect<Scalar>();
                var components = taskRefString.Value.Split('@');

                var taskReference = new TaskReference
                {
                    Name = components[0],
                };

                if (components.Length == 2)
                {
                    taskReference.Version = components[1];
                }

                String name = null;
                var inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                while (parser.Peek<MappingEnd>() == null)
                {
                    var nextProperty = parser.Expect<Scalar>();
                    if (nextProperty.Value.Equals("name"))
                    {
                        name = parser.Expect<Scalar>().Value;
                    }
                    else if (nextProperty.Value.Equals("inputs"))
                    {
                        inputs = parser.ReadMappingOfStringString();
                    }
                }

                step = new TaskStep
                {
                    Name = name,
                    Inputs = inputs,
                    Reference = taskReference,
                };
            }
            else if (stepType.Value.Equals("export"))
            {
                while (parser.Peek<MappingEnd>() == null)
                {
                    parser.MoveNext();
                }

                step = new ExportStep();
            }
            else
            {
                throw new SyntaxErrorException(stepType.Start, stepType.End, $"Unknown step type {stepType.Value}");
            }

            parser.Expect<MappingEnd>();
            return step;
        }

        public void WriteYaml(
            IEmitter emitter,
            Object value,
            Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class PipelineValueYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(PipelineValue).IsAssignableFrom(type);
            // if (type == typeof(String) ||
            //     type == typeof(String[]) ||
            //     type == typeof(List<String>) ||
            //     type == typeof(Dictionary<String, String>))
            // {
            //     Console.WriteLine($"PipelineValue accepts {type.Name}");
            //     return true;
            // }

            // Console.WriteLine($"PipelineValue not accepts {type.Name}");
            // return false;
        }

        public Object ReadYaml(
            IParser parser, 
            Type type)
        {
            var stringType = parser.Allow<Scalar>();
            if (stringType != null)
            {
                Console.WriteLine($"Read PipelineValue from String: '{stringType.Value}'");
                return new StringValue(stringType.Value);
            }

            var stringArray = parser.Allow<SequenceStart>();
            if (stringArray != null)
            {
                PipelineValue retVal = null;

                if (parser.Accept<MappingStart>())
                {
                    var items = new List<IDictionary<String, String>>();
                    while (!parser.Accept<SequenceEnd>())
                    {
                        items.Add(parser.ReadMappingOfStringString());
                    }

                    Console.WriteLine($"Read PipelineValue from StringDictionaryArrayValue");
                    retVal = new StringDictionaryArrayValue(items);
                }
                else if (parser.Accept<Scalar>())
                {
                    var items = new List<string>();
                    while (!parser.Accept<SequenceEnd>())
                    {
                        items.Add(parser.Expect<Scalar>().Value);
                    }

                    Console.WriteLine($"Read PipelineValue from StringArrayValue");
                    retVal = new StringArrayValue(items);
                }

                parser.Expect<SequenceEnd>();

                return retVal;
            }

            var mapping = parser.Allow<MappingStart>();
            if (mapping != null)
            {
                Console.WriteLine($"Read PipelineValue from StringDictionaryValue");
                return new StringDictionaryValue(parser.ReadMappingOfStringString());
            }

            Console.WriteLine("Unable to read PipelineValue");
            return null;
        }

        public void WriteYaml(
            IEmitter emitter, 
            Object value, 
            Type type)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class VariableGroupTemplateYamlConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(VariableGroupTemplate).Equals(type);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.Accept<Scalar>())
            {
                // ex. {{ ref to some named variable group }}
                // won't have this block at all if go mustache route
                return new VariableGroupTemplate(parser.Expect<Scalar>().Value);
            }
            else
            {
                return new VariableGroupTemplate(parser.ReadMappingOfStringString());
            }
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }
}