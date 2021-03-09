#include <map>
#include <vector>
#include "tesselator.h"
#include "tessbridge.h"



class JobData {
public:
    std::vector<float> VertexData;
    std::vector<float> NormalData;
    std::vector<int32_t> Indices;
};

std::map<int, JobData*> Jobs;
int JobIdCounter = 0;


void collect_result(int32_t JobId, float* OutVertexBuffer, float* OutNormalBuffer, int32_t* OutIndexBuffer)
{
    auto Job = Jobs[JobId];

    auto VertexDataSize = Job->VertexData.size();
    auto NormalDataSize = Job->NormalData.size();
    auto IndexDataSize = Job->Indices.size();
    std::memcpy(OutVertexBuffer, Job->VertexData.data(), VertexDataSize * sizeof(float));
    std::memcpy(OutNormalBuffer, Job->NormalData.data(), NormalDataSize * sizeof(float));
    std::memcpy(OutIndexBuffer, Job->Indices.data(), IndexDataSize * sizeof(int32_t));
    Jobs.erase(JobId);
    delete Job;
}



int32_t tessellate(float* InVertexData, float* InNormalData, int32_t* InContourVertexCounts, int32_t ContourCount,
    int32_t& OutVertexCount, int32_t& OutNormalCount, int32_t& OutIndexCount)
{
    auto tess = tessNewTess(nullptr);
    int32_t JobId = -1;
    size_t VertexOffset = 0;
    bool bContourFound = false;

    for (auto ContourIndex = 0; ContourIndex < ContourCount; ContourIndex++)
    {
        int ContourVertexCount = InContourVertexCounts[ContourIndex];
        if (ContourVertexCount < 3)
        {
            // Skip degenerate contour with less than 3 vertices
            VertexOffset += ContourVertexCount;
            continue;
        }
        //, 
        tessAddContour(tess, 3, InVertexData + (VertexOffset * 3), 3 * sizeof(float), ContourVertexCount);
        VertexOffset += ContourVertexCount;

        bContourFound = true;
    }

    if (bContourFound)
    {
        if (tessTesselate(tess, TESS_WINDING_ODD, TESS_POLYGONS, 3, 3, nullptr)) {

            JobData* Job = new JobData();
            size_t TessellatedVertexParameterCount = tessGetVertexCount(tess);
            Job->VertexData.resize(3 * TessellatedVertexParameterCount);

            const auto* TessellatedVertexData = tessGetVertices(tess);
            std::memcpy(Job->VertexData.data(), TessellatedVertexData, 3 * TessellatedVertexParameterCount * sizeof(float));
            

            const auto* TessellatedVertexIndexMap = tessGetVertexIndices(tess);
            Job->NormalData.resize(Job->VertexData.size());
            for (size_t i = 0; i < TessellatedVertexParameterCount; i++)
            {
                if (TessellatedVertexIndexMap[i] != TESS_UNDEF) 
                {
                    size_t ix = TessellatedVertexIndexMap[i];
                    VertexOffset = 0;
                    for (auto c = 0; c < ContourCount; c++) 
                    {
                        size_t ContourVertexCount = InContourVertexCounts[c];
                        if (ix < ContourVertexCount) 
                        {
                            Job->NormalData[3 * i + 0] = InNormalData[3 * ix + 0 + VertexOffset];
                            Job->NormalData[3 * i + 1] = InNormalData[3 * ix + 1 + VertexOffset];
                            Job->NormalData[3 * i + 2] = InNormalData[3 * ix + 2 + VertexOffset];
                            break;
                        }
                        ix -= ContourVertexCount;
                        VertexOffset += ContourVertexCount * 3;
                    }
                }
            }

            const auto* TessellatedTriangles = tessGetElements(tess);
            int TessellatedTriangleCount = tessGetElementCount(tess);
            for (size_t TriangleIndex = 0; TriangleIndex < TessellatedTriangleCount; TriangleIndex++) {
                auto* TrianglePtr = TessellatedTriangles + 3 * TriangleIndex;
                if ((TrianglePtr[0] != TESS_UNDEF) && (TrianglePtr[1] != TESS_UNDEF) && (TrianglePtr[2] != TESS_UNDEF)) {
                    Job->Indices.push_back(TrianglePtr[0]);
                    Job->Indices.push_back(TrianglePtr[1]);
                    Job->Indices.push_back(TrianglePtr[2]);
                }
            }

            JobId = JobIdCounter++;
            Jobs[JobId] = Job;
            OutVertexCount = (int32_t)Job->VertexData.size();
            OutNormalCount = (int32_t)Job->NormalData.size();
            OutIndexCount = (int32_t)Job->Indices.size();
        }
    }
    tessDeleteTess(tess);

    return JobId;
}
