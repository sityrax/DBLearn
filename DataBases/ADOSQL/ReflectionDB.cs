using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Data;
using System.Linq;
using System;

class ReflectionDB<T> where T : class, new()
{
    public readonly T instance;
    public string TableName { get; private set; }
    public PropertyInfoDB PrimaryKey { get; private set; }  // TODO: может быть несколько полей.
    public PropertyInfoDB[] PropertiesInfoDB { get; private set; }
    public PropertyInfo[] Properties { get; private set; }
    public Dictionary<string, PropertyInfoDB> PropertiesRelations { get; private set; }
    public Dictionary<Type, string> DefaultMSSQL_TypeMatching { get; private set; }
    public Dictionary<Type, SqlDbType> SqlDbTypeRelations { get; private set; }
    public Dictionary<string, string> PropertуNames { get; private set; }

    public ReflectionDB()
    {
        instance = new();
        Properties = instance.GetType()
                             .GetProperties();

        AssignPropertiesDB();
        AssignDefaultTypes();
        AssignPropertyNames();
        AssignSqlDbTypeRelations();
        FindPrimaryKey();
        GetTableName();
    }

    private void AssignPropertyNames()
    {
        PropertуNames = new();
        foreach (var property in PropertiesInfoDB)
        {
            PropertуNames.Add(property.Name, property.dbName);
        }
    }

    private void FindPrimaryKey()
    {
        PrimaryKey = PropertiesInfoDB.Where(x => x.PrimaryKey is true).First();
    }

    private void GetTableName()
    {
        Type instanceType = instance.GetType();
        TableAttribute tableAttribute = instanceType.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;
        if (tableAttribute is not null)
            TableName = tableAttribute.Name;
        else
            TableName = instanceType.Name.Split('.').Last();
    }

    #region Read/Write Zone
    public T ReflectionRead(SqlDataReader reader)
    {
        T localInstance = new();
        for (int i = 0; i < Properties.Length; i++)
        {
            string propertyName = PropertуNames[Properties[i].Name];
            Properties[i].SetValue(localInstance, reader[propertyName]);
        }
        return localInstance;
    }

    public T ReflectionRead(DataRow row)
    {
        T localInstance = new();
        for (int i = 0; i < Properties.Length; i++)
        {
            string propertyName = PropertуNames[Properties[i].Name];
            Properties[i].SetValue(localInstance, row[propertyName]);
        }
        return localInstance;
    }

    public DataRow ReflectionWrite(T source, DataRow destination)
    {
        for (int i = 0; i < Properties.Length; i++)
        {
            object value = Properties[i].GetValue(source);
            if (PropertiesRelations[Properties[i].Name] == PrimaryKey)
                destination[PropertуNames[Properties[i].Name]] = value ?? DBNull.Value;
            else
                destination[PropertуNames[Properties[i].Name]] = value;
        }
        return destination;
    }
    #endregion

    #region Assign
    private void AssignPropertiesDB()
    {
        PropertiesInfoDB = new PropertyInfoDB[Properties.Length];
        PropertiesRelations = new Dictionary<string, PropertyInfoDB>();
        for (int i = 0; i < Properties.Length; i++)
        {
            PropertyInfoDB propertyInfoDB = new(Properties[i]);
            PropertiesInfoDB[i] = propertyInfoDB;
            PropertiesRelations.Add(Properties[i].Name, propertyInfoDB);
        }
    }

    private void AssignDefaultTypes()
    {
        DefaultMSSQL_TypeMatching = new Dictionary<Type, string>();

        DefaultMSSQL_TypeMatching.Add(typeof(int), "INTEGER");
        DefaultMSSQL_TypeMatching.Add(typeof(int?), "INTEGER");
        DefaultMSSQL_TypeMatching.Add(typeof(float), "REAL");
        DefaultMSSQL_TypeMatching.Add(typeof(double), "FLOAT");
        DefaultMSSQL_TypeMatching.Add(typeof(decimal), "NUMERIC(10,2)");
        DefaultMSSQL_TypeMatching.Add(typeof(DateTime), "DATETIME");
        DefaultMSSQL_TypeMatching.Add(typeof(string), "NTEXT");

        foreach (var propertyInfo in PropertiesInfoDB)
        {
            propertyInfo.dbType ??= DefaultMSSQL_TypeMatching[propertyInfo.type];
        }
    }

    private void AssignSqlDbTypeRelations()
    {
        SqlDbTypeRelations = new Dictionary<Type, SqlDbType>();

        SqlDbTypeRelations.Add(typeof(int), SqlDbType.Int);
        SqlDbTypeRelations.Add(typeof(int?), SqlDbType.Int);
        SqlDbTypeRelations.Add(typeof(float), SqlDbType.Real);
        SqlDbTypeRelations.Add(typeof(double), SqlDbType.Float);
        SqlDbTypeRelations.Add(typeof(decimal), SqlDbType.Decimal);
        SqlDbTypeRelations.Add(typeof(DateTime), SqlDbType.DateTime);
        SqlDbTypeRelations.Add(typeof(string), SqlDbType.NVarChar);

        foreach (var propertyInfo in PropertiesInfoDB)
        {
            propertyInfo.sqlDbType = SqlDbTypeRelations[propertyInfo.type];
        }
    }
    #endregion

    /// <param name="obj">The instance whose property value is to be received.</param>
    /// <returns>Returns a PropertyInfoDB collection with values.</returns>
    public PropertyInfoDB[] LoadValues(T obj)
    {
        for (int i = 0; i < Properties.Length; i++)
        {
            object value = Properties[i].GetValue(obj);
            if (PropertiesRelations[Properties[i].Name] == PrimaryKey)
                PropertiesInfoDB[i].value = value ?? DBNull.Value;
            else
                PropertiesInfoDB[i].value = value;
        }
        return PropertiesInfoDB;
    }

    public bool Equals(T entity, DataRow rows)
    {
        foreach (PropertyInfo property in Properties)
        {
            if (!PropertiesRelations[property.Name].PrimaryKey)
            {
                string name = PropertiesRelations[property.Name].dbName;
                object obj = property.GetValue(entity);
                Type objType = obj.GetType();
                if (objType == typeof(DateTime))    // дата не желает сравниваться по канону, поэтому выделяем ей отдельный пассаж.
                    if (obj.ToString() != rows[name].ToString())
                        return false;
                    else
                        continue;
                if (!obj.Equals(rows[name]))
                    return false;
            }
        }
        return true;
    }
}

class PropertyInfoDB
{
    public string Name;
    public string dbName;
    public bool Required;
    public bool PrimaryKey;
    public byte? Precision;
    public byte? Scale;
    public int Size;
    public object value;
    public Type type;
    public string dbType;
    public SqlDbType sqlDbType;

    public PropertyInfoDB(PropertyInfo property)
    {
        Name = property.Name;

        this.GetDbName(property)
            .FindPrimaryKey(property)
            .AssignPropertyRequired(property)
            .AssignPropertyTypes(property);
    }

    private PropertyInfoDB GetDbName(PropertyInfo propertyInfo)
    {
        var propertyAttributesCollection = propertyInfo.GetCustomAttribute(typeof(ColumnAttribute));
        if (propertyAttributesCollection is not null)
        {
            var propertyAttributeName = (propertyAttributesCollection as ColumnAttribute).Name;
            dbName = propertyAttributeName;
        }
        return this;
    }

    private PropertyInfoDB FindPrimaryKey(PropertyInfo propertyInfo)
    {
        if (propertyInfo.GetCustomAttribute(typeof(KeyAttribute)) is not null)
        {
            PrimaryKey = true;
        }
        return this;
    }

    private PropertyInfoDB AssignPropertyRequired(PropertyInfo propertyInfo)
    {
        var requiredAttribute = propertyInfo.GetCustomAttribute(typeof(RequiredAttribute));
        var keyAttribute = propertyInfo.GetCustomAttribute(typeof(KeyAttribute));
        if (requiredAttribute is not null || keyAttribute is not null)
            Required = true;
        return this;
    }

    private void AssignPropertyTypes(PropertyInfo propertyInfo)
    {
        type = propertyInfo.PropertyType;
        ColumnAttribute columnAttribute = (propertyInfo.GetCustomAttribute(typeof(ColumnAttribute))) as ColumnAttribute; // чекаем атрибуты.
        string typeName = columnAttribute?.TypeName;
        dbType = typeName; 

        string pattren = @"([(]{1}(\d{1,2}[,]?\d{0,2})[)]{1}$)"; // ищем что-то типа "(10,5)" или "(64)" в конце строки"
        Regex regex = new(pattren);
        Match match = regex.Match(dbType);
        string group = match.Groups[2].ToString();
        string[] digits = group.Split(",");
        if (digits.Length > 0 && digits[0] != string.Empty) // в атрибутах есть чем поживиться.
        {
            if (digits.Length == 2)
            {
                Scale     = byte.Parse(digits[0]);
                Precision = byte.Parse(digits[1]);
            }
            if (digits.Length == 1)
                Size = byte.Parse(digits[0]);
        }
    }
}
