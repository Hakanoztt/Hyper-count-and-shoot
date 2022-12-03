namespace Mobge.HyperCasualSetup.RoadGenerator {

    interface EIRoadElement {

        BaseRoadElementComponent.Data DataObjectT { get; }
        void UpdateData();
    }
}
