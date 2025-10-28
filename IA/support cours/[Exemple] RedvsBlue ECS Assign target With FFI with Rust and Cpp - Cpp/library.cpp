

#include <cstdint>
#include <cfloat>
#include <cmath>

#if WIN32
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT
#endif

struct float3 {
    float x;
    float y;
    float z;
};

extern "C" {

    DLLEXPORT float3* returnMyStruct()
    {
        auto elt = new float3();
        elt->x = 51.0;
        elt->y = 64.0;
        elt->z = 90.0;

        return elt;
    }

    DLLEXPORT void deleteMyStruct(float3* elt)
    {
        delete elt;
    }


    DLLEXPORT int32_t return42() {
        return 42;
    }

    DLLEXPORT int32_t my_add(int32_t a, int32_t b) {
        return a + b;
    }

    DLLEXPORT void compute_targets(
            struct float3* sourcePositionsArray,
            int32_t sourcePositionsArrayLength,
            struct float3* targetPositionsArray,
            int32_t targetPositionsArrayLength,
            int32_t* resultIndices)
    {
        for (auto i = 0; i < sourcePositionsArrayLength; i++) {
            auto best_distance = FLT_MAX;
            auto best_index = 0;
            auto source_pos = sourcePositionsArray[i];

            for (auto j = 0; j < targetPositionsArrayLength; j++) {
                auto target_pos = targetPositionsArray[j];
                auto xdiff = target_pos.x - source_pos.x;
                auto ydiff = target_pos.y - source_pos.y;
                auto zdiff = target_pos.z - source_pos.z;
                auto squared_distance = xdiff * xdiff + ydiff * ydiff + zdiff * zdiff;

                if (squared_distance <= best_distance) {
                    best_index = j;
                    best_distance = squared_distance;
                }
            }

            resultIndices[i] = best_index;
        }
    }
}