using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AspectCore.Extensions.Reflection;
using DotNext.Reflection;
using BenchmarkDotNet.Attributes;

namespace KaneBlake.Basis.Performance.Benchmarks
{
    public class ReflectionSetterBenchmark
    {
        private class IndexerEntity
        {
            public string Prop1 { get; set; }
            public int Prop2 { get; set; }
            public decimal Prop3 { get; set; }
            public IndexerEntity(string prop1, int prop2, decimal prop3)
            {
                Prop1 = prop1;
                Prop2 = prop2;
                Prop3 = prop3;
            }
        }
        private readonly IndexerEntity _indexerEntity;
        private readonly PropertyReflector _reflector;
        private readonly MemberSetter<IndexerEntity, string> _dotNextSetter;
        public ReflectionSetterBenchmark()
        {
            _indexerEntity = new("hello word.", 1, 0.99M);
            var property = typeof(IndexerEntity).GetTypeInfo().GetProperty("Prop1");
            _reflector = property.GetReflector();
            _dotNextSetter = Type<IndexerEntity>.Property<string>.RequireSetter(nameof(IndexerEntity.Prop1));
        }


        [Benchmark]
        public void DirectCallSetter()
        {
            _indexerEntity.Prop1 = "hello word!";
        }
        [Benchmark]
        public void AspectCoreSetter()
        {
            _reflector.SetValue(_indexerEntity,"hello word!");
        }
        [Benchmark]
        public void DotNextSetter()
        {
            _dotNextSetter(_indexerEntity, "hello word!");
        }
    }
}
