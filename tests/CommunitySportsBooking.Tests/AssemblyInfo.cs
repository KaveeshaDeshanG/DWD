// All test classes in this project hit the same real, shared CommunitySportsBookingDB
// (by design — constitution Principle IV, real-DB testing over mocks). xUnit
// parallelizes different test classes by default, which is unsafe here: one
// class's row-count assertions can observe another class's still-in-flight
// booking/member rows mid-test. Putting every test class in the same named
// collection makes xUnit run them sequentially relative to each other,
// eliminating that whole category of flaky failure — caught for real when
// DataAccessSmokeTests failed a row-count assertion while
// BookingFunctionalityTests was concurrently creating and cleaning up rows.
[CollectionDefinition("Database collection")]
public class DatabaseCollection
{
}
