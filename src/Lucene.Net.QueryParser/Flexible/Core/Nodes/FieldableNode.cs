using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A query node implements {@link FieldableNode} interface to indicate that its
    /// children and itself are associated to a specific field.
    /// 
    /// If it has any children which also implements this interface, it must ensure
    /// the children are associated to the same field.
    /// </summary>
    public interface IFieldableNode : IQueryNode
    {
        /// <summary>
        /// Gets or Sets the field associated to the node and every node under it.
        /// </summary>
        string Field { get; set; }

        //     /**
        //* Returns the field associated to the node and every node under it.
        //* 
        //* @return the field name
        //*/
        //     string GetField(); 

        //     /**
        //      * Associates the node to a field.
        //      * 
        //      * @param fieldName
        //      *          the field name
        //      */
        //     void SetField(string fieldName);
    }
}
