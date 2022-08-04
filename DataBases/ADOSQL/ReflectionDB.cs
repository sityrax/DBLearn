using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Data;
using System;

class ReflectionDB<T> where T : class, new()
{
    public readonly T instance;
    public PropertyInfo PrimaryKey { get; private set; }
    public PropertyInfo[] Properties { get; private set; }
    /// <summary> Relationship between property names in application and database.</summary>
    public Dictionary<string, string> PropertуNames { get; private set; }
    public Dictionary<PropertyInfo, string> PropertуTypes { get; private set; }
    public Dictionary<PropertyInfo, string> PropertуRequired { get; private set; }
    public Dictionary<Type, string> DefaultMSSQL_TypeMatching { get; private set; }

    public ReflectionDB()
    {
        instance = new();
        Properties = instance.GetType()
                             .GetProperties();
        PropertуNames = new Dictionary<string, string>();

        foreach (var property in Properties)
        {
            var propertyAttributesCollection = property.GetCustomAttribute(typeof(ColumnAttribute));
            if (propertyAttributesCollection is not null)
            {
                var propertyAttributeName = (propertyAttributesCollection as ColumnAttribute).Name;
                PropertуNames.Add(property.Name, propertyAttributeName);
            }
        }

        FindPrimaryKey();
        AssignDefaultTypes();
        AssignPropertyTypes();
        AssignPropertyRequired();
    }

    private void FindPrimaryKey()
    {
        foreach (var property in Properties)
        {
            if (property.GetCustomAttribute(typeof(KeyAttribute)) is not null)
            {
                PrimaryKey = property;
                return;
            }
        }
    }

    #region Read/Write Zone
    public T ReflectionRead(SqlDataReader reader)
    {
        T localInstance = new();
        PropertyInfo[] localProperties = localInstance.GetType()
                                                      .GetProperties();
        for (int i = 0; i < localProperties.Length; i++)
        {
            string propertyName = PropertуNames[localProperties[i].Name];
            localProperties[i].SetValue(localInstance, reader[propertyName]);
        }
        return localInstance;
    }

    public T ReflectionRead(DataRow row)
    {
        T localInstance = new();
        PropertyInfo[] localProperties = localInstance.GetType()
                                                      .GetProperties();
        for (int i = 0; i < localProperties.Length; i++)
        {
            string propertyName = PropertуNames[localProperties[i].Name];
            localProperties[i].SetValue(localInstance, row[propertyName]);
        }
        return localInstance;
    }

    public DataRow ReflectionWrite(T source, DataRow destination)
    {
        PropertyInfo[] localProperties = source.GetType()
                                               .GetProperties();
        for (int i = 0; i < localProperties.Length; i++)
        {
            destination[PropertуNames[localProperties[i].Name]] = localProperties[i].GetValue(source);
        }
        return destination;
    }
    #endregion

    #region Assign
    private void AssignPropertyRequired()
    {
        PropertуRequired = new();
        foreach (var property in Properties)
        {
            var requiredAttribute = property.GetCustomAttribute(typeof(RequiredAttribute));
            var keyAttribute      = property.GetCustomAttribute(typeof(KeyAttribute));
            if (requiredAttribute is not null || keyAttribute is not null)
                PropertуRequired.Add(property, "NOT NULL");
            else
                PropertуRequired.Add(property, "NULL");
        }
    }

    private void AssignPropertyTypes()
    {
        PropertуTypes = new();
        foreach (var property in Properties)
        {
            ColumnAttribute columnAttribute = (property.GetCustomAttribute(typeof(ColumnAttribute))) as ColumnAttribute; // чекаем атрибуты.
            string typeName = columnAttribute?.TypeName;
            if (typeName is not null)
                PropertуTypes.Add(property, typeName);  // в атрибутах есть чем поживиться.
            else
                PropertуTypes.Add(property, DefaultMSSQL_TypeMatching[property.GetType()]); // в атрибутах кот наплакал.
        }
    }

    private void AssignDefaultTypes()
    {
        DefaultMSSQL_TypeMatching = new Dictionary<Type, string>();

        DefaultMSSQL_TypeMatching.Add(typeof(int), "INTEGER");
        DefaultMSSQL_TypeMatching.Add(typeof(float), "REAL");
        DefaultMSSQL_TypeMatching.Add(typeof(double), "FLOAT");
        DefaultMSSQL_TypeMatching.Add(typeof(decimal), "NUMERIC(10,5)");
        DefaultMSSQL_TypeMatching.Add(typeof(DateTime), "DATETIME");
        DefaultMSSQL_TypeMatching.Add(typeof(string), "NTEXT");
    }
    #endregion

    public bool Equals(T entity, DataRow rows)
    {
        PropertyInfo[] properties = entity.GetType()
                                          .GetProperties();

        foreach (PropertyInfo property in properties)
        {
            ColumnAttribute columnAttribute = (property.GetCustomAttribute(typeof(ColumnAttribute))) as ColumnAttribute;
            string name = columnAttribute?.Name;
            if (!property.GetValue(entity).Equals(rows[name]))
                return false;
        }
        return true;
    }
}
