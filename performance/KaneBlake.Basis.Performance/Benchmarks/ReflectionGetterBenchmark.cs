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
    public class ReflectionGetterBenchmark
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
        private readonly MemberGetter<IndexerEntity, string> _dotNextGetter;
        private MemberGetter<IndexerEntity, string> _dotNextGetter2;
        private PropertyInfo _property;
        public ReflectionGetterBenchmark()
        {
            _indexerEntity = new("hello word.", 1, 0.99M);
            var property = typeof(IndexerEntity).GetTypeInfo().GetProperty("Prop1");
            _property = typeof(IndexerEntity).GetTypeInfo().GetProperty("Prop1");
            _reflector = property.GetReflector();
            _dotNextGetter = Type<IndexerEntity>.Property<string>.RequireGetter(nameof(IndexerEntity.Prop1));
            DynamicInvoker a;
        }

        [Benchmark]
        public string DirectCallGetter()
        {
            return _indexerEntity.Prop1;
        }
        [Benchmark]
        public object AspectCoreGetter()
        {
            return _reflector.GetValue(_indexerEntity);
        }
        [Benchmark]
        public object AspectCoreGetter2()
        {
            return typeof(IndexerEntity).GetTypeInfo().GetProperty("Prop1").GetReflector().GetValue(_indexerEntity);
        }
        [Benchmark]
        public object AspectCoreGetter3()
        {
            return _property.GetReflector().GetValue(_indexerEntity);
        }
        [Benchmark]
        public string DotNextGetter()
        {
            return _dotNextGetter(_indexerEntity);
        }
        [Benchmark]
        public string DotNextGetter2()
        {

            _dotNextGetter2 = Type<IndexerEntity>.Property<string>.RequireGetter(nameof(IndexerEntity.Prop1));
            return _dotNextGetter2(_indexerEntity);
        }
    }
}
