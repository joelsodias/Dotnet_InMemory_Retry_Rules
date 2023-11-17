

static class DataFactory {

    public static List<Source> GetSourceDatasetFresh(int min, int max) {

        Random random = new Random();

        var count = random.Next(min, max);

        var list = new List<Source>();

        for (int i = 0; i < count; i++) list.Add(new Source("myDataSourceSample", "Scheduled", 0, null, null,  "RFB never executed")); 

        return list;
    }


}