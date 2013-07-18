#include "UnitTest++.h"
#include "StringHelper.h"

TEST(MyTest)
{
    bool result = StringHelper::BeginsWithIgnoreCase(U("hello"), U("hello"));
    CHECK(result);
}

int main()
{
    return UnitTest::RunAllTests();
}