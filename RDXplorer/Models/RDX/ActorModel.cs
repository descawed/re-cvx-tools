﻿namespace RDXplorer.Models.RDX
{
    public class ActorModel : DataModel<ActorModelFields> { }

    public class ActorModelFields : IFieldsModel
    {
        public DataEntryModel<int> Header { get; set; } = new();
        public DataEntryModel<short> Type { get; set; } = new();
        public DataEntryModel<short> Effect { get; set; } = new();
        public DataEntryModel<short> Variant { get; set; } = new();
        public DataEntryModel<short> Index { get; set; } = new();
        public DataEntryModel<float> X { get; set; } = new();
        public DataEntryModel<float> Y { get; set; } = new();
        public DataEntryModel<float> Z { get; set; } = new();
        public DataEntryModel<int> XRotation { get; set; } = new();
        public DataEntryModel<int> YRotation { get; set; } = new();
        public DataEntryModel<int> ZRotation { get; set; } = new();
    }
}