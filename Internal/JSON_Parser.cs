﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JsonUnitSimplifier
{
    internal class JSON_Parser
    {      
        internal static UnitTest Parse(string json)
        {
            // Получение джейсон строки
            try
            {
                json = File.ReadAllText(json);
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.GetType().Name + ": can't find or read file, trying to parse json as json string");
            }            

            // Десеарилизация
            var test = JsonConvert.DeserializeObject<UnitTest>(json);

            // Чтобы без ошыбок пустоко массива
            if (test.rules == null)
                test.rules = new List<Rule>();

            // Обработка правил в объекте после парсинга классы см. ниже
            foreach (var rule in test.rules)
            {
                rule.try_update_type_of_fields();
            }

            // Убираем ошибки нуля, ставим значения по умолчанию
            if (test.assert_before_lambda != null && test.assert_before_lambda.Count > 0)
            {
                foreach (var assert in test.assert_before_lambda)
                {
                    if (assert.type_assert == null)
                        assert.type_assert = "equals";
                    if (assert.args == null)
                        assert.args = EMPTY_LIST;
                }
            }
            if (test.assert_after_lambda != null && test.assert_after_lambda.Count > 0)
            {
                foreach (var assert in test.assert_after_lambda)
                {
                    if (assert.type_assert == null)
                        assert.type_assert = "equals";
                    if (assert.args == null)
                        assert.args = EMPTY_LIST;
                }
            }

            return test;
        }

        private static List<object> EMPTY_LIST = new List<object>();
    }

    internal class UnitTest
    {
        public string id { get; set; }
        public string mode { get; set; }
        public string combination_mode { get; set; }
        public List<string> classes { get; set; }
        public List<Rule> rules { get; set; }
        public List<Assert> assert_before_lambda { get; set; }
        public List<Assert> assert_after_lambda { get; set; }
    }

    internal class Rule
    {
        public string field { get; set; }
        public List<object> values { get; set; }
        public object value { get; set; }
        public List<double> range { get; set; }
        public double? step { get; set; }
        public string function { get; set; }
        public int? function_calls { get; set; }
        public string field_type { get; set; }
        public object random { get; set; }

        public void try_update_type_of_fields()
        {
            if (field_type == null)
                return;

            try
            {
                Type type = Type.GetType(field_type);
                if (type == null)
                    throw new Exception($"Type {field_type} not found.");

                if (value != null)
                {
                    value = Convert.ChangeType(value, type);
                }
                else if (values != null)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        values[i] = value = Convert.ChangeType(values[i], type);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot cast to type {field_type}: {ex.Message}");
            }
        }

        public int Combinations { get 
            {
                if (values != null)
                    return values.Count;
                if (value != null)
                    return 1;
                if (range != null && step != null)
                    return (int) ((range[1] - range[0]) / step + 1);
                if (function != null)
                {
                    if (function_calls != null)
                        return (int)function_calls;
                    return 1;
                }
                else if (random != null)
                {
                    if (function_calls != null)
                        return (int)function_calls;
                    return 1;
                }

                throw new Exception("I am a teapot");
            } 
        }
    }

    internal class Assert
    {
        public string method { get; set; }
        public string function { get; set; }
        public string target { get; set; }
        public List<object> args { get; set; }
        public string exception { get; set; }
        public List<string> exceptions { get; set; }
        public List<object> results { get; set; }
        public object result { get; set; }
        public string type_assert { get; set; }
        public string field { get; set; }
        public object value { get; set; }
        public List<object> values { get; set; }
    }
}
