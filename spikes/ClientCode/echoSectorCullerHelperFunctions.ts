import { CadModelBudget } from '@cognite/reveal';
import {
    DetermineSectorsInput,
    SectorCost
} from '@cognite/reveal/dist/packages/cad-geometry-loaders/src/sector/culling/types';
import { CadModelMetadata, SectorScene } from '@cognite/reveal/dist/packages/cad-parsers';
import { SectorMetadata } from '@cognite/reveal/dist/packages/cad-parsers/src/metadata/types';
import { PrioritizedArea } from '@cognite/reveal/dist/packages/cad-styling';
import * as THREE from 'three';
import { EchoSectorMap } from '../EchoSectorMap';
import { CutoffZone, EchoWeightFunctions } from '../EchoWeightFunctions';
import { SectorZonePriority } from './SectorZonePriority';

export enum LevelOfDetail {
    Discarded,
    Simple,
    Detailed
}
const preallocated = {
    transformedBounds: new THREE.Box3()
};

/* istanbul ignore next */
/**
 * determine the sector priority
 *   The idea:
 *     DistanceZone 0: - Everything is prioritized
 *                      - Values in the range 100000-distance
 *     DistanceZone 1: - Prioritize sectors of depth 1 higher than the rest
 *                      - Values in the range 80000-distance*2 for depth 1
 *                      - Values in the range 50000-distance*4 for depth >1
 *     DistanceZone 2: - Low priority, only distance as a factor
 *                      - Values in the range 20000-depth*100-dist*100
 *
 * @param {EchoWeightFunctions} weightFunctions The functions containing actual weighting
 * @param {SectorMetadata} sector The sector to analyze weighting for
 * @param {THREE.Box3} transformedBounds The bounds of the sector being analyzed
 * @param {PrioritizedArea[]} prioritizedAreas Areas to prioritize, currently not in use
 * @param {boolean} isCameraInMotion Is the camera in motion
 * @returns {number} priorityWeight The calculated weight of the sector
 */
export function determineSectorPriorityEcho(
    weightFunctions: EchoWeightFunctions,
    sector: SectorMetadata,
    transformedBounds: THREE.Box3,
    prioritizedAreas: PrioritizedArea[],
    // eslint-disable-next-line @typescript-eslint/no-unused-vars -- Potential future use
    isCameraInMotion: boolean
): number {
    // This code assumes that candidate sectors are already frustum-culled when determining priority.
    // We never check if a sector is behind the camera and can assume distances are forward

    if (prioritizedAreas.length > 0) {
        /* istanbul ignore next */
        // eslint-disable-next-line no-console -- Logging an unhandled (yet not critical) situation
        console.log('WARNING! Got prioritizedAreas as input to determineSectorPriority, currently not handled!');
    }
    // Split the sectors into Zones based on distance from camera (cutoff)
    const weight = weightFunctions.computeZoneAndDistance(
        transformedBounds,
        SectorZonePriority.nearDistanceCutoffMeters,
        SectorZonePriority.middleDistanceCutoffMeters
    ); // 0 < CutOff1, 1 < Cutoff2, 2>The rest

    const { distance } = weight;
    const { zone } = weight;
    const sectorDepth = sector.depth;
        //  return 1;

        const percentOfScreenFilledByLargestNodeX = weightFunctions.computeMaximumNodeScreenSize(
            distance,
            sector.maxDiagonalLength
        );
        // if(sector.sectorFileName?.endsWith("_1_shadow.glb"))
        //     return 0;
        // if(sector.sectorFileName?.endsWith("shadow.glb"))
        //     return 1;
        //     return 0;
        // if(sector.sectorFileName?.endsWith("_1.glb"))
        //     return 1;
        //     // if(sector.sectorFileName?.includes("_1."))
        //     // return 1;
        //  return 0;

/*
if(zone===CutoffZone.Near || zone===CutoffZone.Middle){
        if(sector.sectorFileName?.includes("shadow")){
            return 0;
        }
        return SectorZonePriority.zonePriorityTop+percentOfScreenFilledByLargestNodeX;
    }
    if(!sector.sectorFileName?.includes("shadow")){
        return 0;
    }
    return SectorZonePriority.zonePriorityLow+percentOfScreenFilledByLargestNodeX;
*/
// if((sectorDepth===3 && distance > 200) || (sectorDepth>3 && distance>50) )
// if(!sector.sectorFileName?.includes("shadow")){
//     return 0;
// }
// return 1;
if(sector.sectorFileName?.endsWith("pri.glb"))
{
    console.log(sector.sectorFileName);
    return 200_000;
}
// return 0;
if((sectorDepth>=3 && distance>20) )
    {
        if(sector.sectorFileName?.endsWith("shadow.glb"))
        {
            return 100_000;
        }
        return 0;
    }

    // Everything closer than cutoff1
    if (zone === CutoffZone.Near) {
        return SectorZonePriority.zonePriorityTop - distance;
    }

    if (sectorDepth <= 2 && zone === CutoffZone.Middle) {
        return SectorZonePriority.zonePriorityHigh - distance * 2;
    }

    // Rough model in Middle
    if (sectorDepth <= 2 && zone === CutoffZone.Middle) {
        return SectorZonePriority.zonePriorityHigh - distance * 2;
    }

    // Everything except rough model closer than cutoff 2
    // If detailed sector CLOSER than X => prioritize higher than further away with lower depth
    if (zone === CutoffZone.Middle) {
        return SectorZonePriority.zonePriorityMedium - sectorDepth * 4 - distance * 10;
    }

    // Load the "coarsest model" to have something to navigate with
    if (sectorDepth === 1) {
        return SectorZonePriority.zonePriorityMedium - distance * 4;
    }

    const percentOfScreenFilledByLargestNode = weightFunctions.computeMaximumNodeScreenSize(
        distance,
        sector.maxDiagonalLength
    );
    if (percentOfScreenFilledByLargestNode < 0.01) {
        // If the largest node would fill less than x percent of the screen at the distance we assume we can discard the sector.
        return 0;
    }

    return 8_000 - percentOfScreenFilledByLargestNode * -1;
}

/**
 * Function to visit all corners of a Box3
 *
 * @param {THREE.Box3} box The box to visit corners of
 * @param {callback} callback Callback with the corner as a Vector3
 */
function visitBox3CornerPoints(box: THREE.Box3, callback: (corner: THREE.Vector3) => void): void {
    const tmpVector = new THREE.Vector3();
    tmpVector.set(box.min.x, box.min.y, box.min.z); // 000
    callback(tmpVector);
    tmpVector.set(box.min.x, box.min.y, box.max.z); // 001
    callback(tmpVector);
    tmpVector.set(box.min.x, box.max.y, box.min.z); // 010
    callback(tmpVector);
    tmpVector.set(box.min.x, box.max.y, box.max.z); // 011
    callback(tmpVector);
    tmpVector.set(box.max.x, box.min.y, box.min.z); // 100
    callback(tmpVector);
    tmpVector.set(box.max.x, box.min.y, box.max.z); // 101
    callback(tmpVector);
    tmpVector.set(box.max.x, box.max.y, box.min.z); // 110
    callback(tmpVector);
    tmpVector.set(box.max.x, box.max.y, box.max.z); // 111
    callback(tmpVector);
}

/**
 * Check all corners to see if the box is in front of the plane
 *
 * @param {THREE.Box3} box The box to check
 * @param {THREE.Plane} plane A plane to check against
 *@returns {boolean} True if the box is (partially) in front of the plane
 */
function isBox3OnPositiveSideOfPlane(box: THREE.Box3, plane: THREE.Plane): boolean {
    let planeAccepts = false;
    visitBox3CornerPoints(box, (boundCorner) => {
        planeAccepts = planeAccepts || plane.distanceToPoint(boundCorner) >= 0;
    });
    return planeAccepts;
}

/**
 *
 * @param {EchoSectorMap} takenSectors The sectorMap
 * @param {CadModelBudget} budget The budget available
 * @param {Array} candidateSectors Array of prioritized candidate sectors
 * @returns {number} The number of sectors chosen
 */
export function takeSectorsWithinBudget(
    takenSectors: EchoSectorMap,
    budget: CadModelBudget,
    candidateSectors: { model: CadModelMetadata; sectorId: number; priority: number }[]
) {
    let takenSectorCount = 0;

    for (let i = 0; i < candidateSectors.length; i++) {
        const { model, sectorId, priority } = candidateSectors[i];
        takenSectors.markSectorDetailed(model, sectorId, priority);
        takenSectorCount = i;
        if (!takenSectors.isWithinBudget(budget)) {
            // When we have exceeded the budget, stop loading
            break;
        }
    }

    // Commented code kept for future debugging purposes
    // const usedBudget = takenSectors.getUsedBudget();
    // console.log(
    //     'Used budget for loaded sectors: - downloadSize: %d drawCalls: %d renderCost: %d',
    //     usedBudget.downloadSize,
    //     usedBudget.drawCalls,
    //     usedBudget.renderCost
    // );

    return takenSectorCount;
}

/**
 * Prepares data structures with model and sectors
 *
 * @param {Map} modelsAndCandidateSectors A Map of model and sectors metadata
 * @param {EchoSectorMap} takenSectors A map of taken sectors
 * @param {EchoWeightFunctions} weightFunctions The weightfunctions to use
 */
export function initializeTakenSectorsAndWeightFunctions(
    modelsAndCandidateSectors: Map<CadModelMetadata, SectorMetadata[]>,
    takenSectors: EchoSectorMap,
    weightFunctions: EchoWeightFunctions
) {
    weightFunctions.reset();
    modelsAndCandidateSectors.forEach((sectors, model) => {
        takenSectors.initializeScene(model);
        weightFunctions.addCandidateSectors(sectors, model.modelMatrix);
    });
}

/**
 * Determines candidate sectors
 *
 * Checks if the sector is visible, and not clipped
 *
 * @param {THREE.Matrix4} cameraWorldInverseMatrix -- The camera world inverse matrix
 * @param {THREE.Matrix4} cameraProjectionMatrix -- The camera projection matrix
 * @param {THREE.Matrix4} modelMatrix -- The model matrix
 * @param {SectorScene} modelScene -- the model scene
 * @param {THREE.Plane[]} clippingPlanes -- The clipping planes
 * @returns {SectorMetadata} candidateSectors
 */
export function determineCandidateSectors(
    cameraWorldInverseMatrix: THREE.Matrix4,
    cameraProjectionMatrix: THREE.Matrix4,
    modelMatrix: THREE.Matrix4,
    modelScene: SectorScene,
    clippingPlanes: THREE.Plane[]
): SectorMetadata[] {
    /* istanbul ignore next */
    if (modelScene.version !== 9) {
        throw new Error(`Expected model version 9, but got ${modelScene.version}`);
    }

    const transformedCameraMatrixWorldInverse = new THREE.Matrix4();
    transformedCameraMatrixWorldInverse.multiplyMatrices(cameraWorldInverseMatrix, modelMatrix);
    const sectors = modelScene
        .getSectorsIntersectingFrustum(cameraProjectionMatrix, transformedCameraMatrixWorldInverse)
        .map((x) => x);

    if (clippingPlanes.length <= 0) {
        return sectors;
    }

    const bounds = new THREE.Box3();
    return sectors.filter((sector) => {
        bounds.copy(sector.subtreeBoundingBox);
        bounds.applyMatrix4(modelMatrix);

        const shouldKeep = clippingPlanes.every((plane) => isBox3OnPositiveSideOfPlane(bounds, plane));
        return shouldKeep;
    });
}
/**
 * Determines candidate sectors per model, i.e. sectors within frustum.
 *
 * @param {CadModelMetadata[]} cadModelsMetadata The CadModels to determine from
 * @param {THREE.Matrix4} cameraWorldInverseMatrix The camera world inverse matrix
 * @param {THREE.Matrix4} cameraProjectionMatrix The camera projection matrix
 * @param {DetermineSectorsInput} input Sector Input
 * @returns {Array} An array of chosen sectors
 */
export function determineCandidateSectorsByModel(
    cadModelsMetadata: CadModelMetadata[],
    cameraWorldInverseMatrix: THREE.Matrix4,
    cameraProjectionMatrix: THREE.Matrix4,
    input: DetermineSectorsInput
) {
    return cadModelsMetadata.reduce((result, model, i) => {
        const sectors = determineCandidateSectors(
            cameraWorldInverseMatrix,
            cameraProjectionMatrix,
            model.modelMatrix,
            model.scene,
            input.modelClippingPlanes[i]
        );
        result.set(model, sectors);
        return result;
    }, new Map<CadModelMetadata, SectorMetadata[]>());
}

/**
 * Sorts all sectors in the input by a priority function
 *
 * MAY discard sectors
 *
 * @param {Map} modelsAndCandidateSectors Map of model and sectors metadata
 * @param {EchoWeightFunctions} weightFunctions The weight functions
 * @param {PrioritizedArea[]} prioritizedAreas Array of prioritized areas
 * @param {boolean} isCameraInMotion Is the camera in motion?
 * @returns {Array} candidateSectors
 */
export function sortSectorsByPriority(
    modelsAndCandidateSectors: Map<CadModelMetadata, SectorMetadata[]>,
    weightFunctions: EchoWeightFunctions,
    prioritizedAreas: PrioritizedArea[],

    isCameraInMotion: boolean
): { model: CadModelMetadata; sectorId: number; priority: number }[] {
    const { transformedBounds } = preallocated;

    const candidateSectors = new Array<{
        model: CadModelMetadata;
        sectorId: number;
        estDrawCalls: number;
        estRenderCost: number;
        priority: number;
    }>();

    const candidateSectors2 = new Array<{
        model: CadModelMetadata;
        sectorId: number;
        estDrawCalls: number;
        estRenderCost: number;
        priority: number;
    }>();

    // Future todo: Add some logic related to rendercost and drawcalls that affects sectors loaded
    modelsAndCandidateSectors.forEach((sectors, model) => {
        sectors.forEach((sectorMetadata) => {
            const sector = sectorMetadata;

            weightFunctions.computeTransformedSectorBounds(
                sector.geometryBoundingBox,
                model.modelMatrix,
                transformedBounds
            );

            const priority = determineSectorPriorityEcho(
                weightFunctions,
                sector,
                transformedBounds,
                prioritizedAreas,
                isCameraInMotion
            );

            // We discard sectors with zero priority as we have determined them as never important
            if (priority > 0) {
                candidateSectors.push({
                    model,
                    sectorId: sector.id,
                    estDrawCalls: sector.estimatedDrawCallCount,
                    estRenderCost: sector.estimatedRenderCost,
                    priority
                });
            }
        });
    });

    candidateSectors.forEach((candidate)=>{
        if(candidate.sectorId>10000)
        {
            const sc = candidate.sectorId-10000;
            const scfound = candidateSectors.findIndex(c=>c.sectorId===sc);
            // console.log(scfound);
            if(scfound>0)
            {
                // console.log('Matched %s with %s',sc,candidateSectors[scfound].sectorId );
                candidateSectors2.push(candidateSectors[scfound]);
            }
            else{
                candidateSectors2.push(candidate);
            }
        }
        else
        if(candidateSectors2.findIndex(c=>c.sectorId===candidate.sectorId)===-1)
            {
                candidateSectors2.push(candidate);
            }
    });
    // console.log('candidates ',candidateSectors.length);
    // console.log('filtered ',candidateSectors2.length);
    // Sort candidate sectors descending by priority
    candidateSectors2.sort((left, right) => {
        return right.priority - left.priority;
    });
    // console.log(candidateSectors2);
    return candidateSectors2;
}

/* istanbul ignore next - No need to test this one */
/**
 * Copy from reveal, gets the cost from the metadata
 *
 * @param {SectorMetadata} sectorMetadata The metadata of a sector
 * @param {LevelOfDetail} lod The level of detail
 * @returns {SectorCost} sectorCost
 */
export function computeSectorCost(sectorMetadata: SectorMetadata, lod: LevelOfDetail): SectorCost {
    const metadata = sectorMetadata;
    switch (lod) {
        case LevelOfDetail.Detailed:
            return {
                downloadSize: metadata.downloadSize,
                drawCalls: metadata.estimatedDrawCallCount,
                renderCost: metadata.estimatedRenderCost
            };
        case LevelOfDetail.Simple:
            throw new Error('Not supported');
        default:
            throw new Error(`Can't compute cost for lod ${lod}`);
    }
}

/* istanbul ignore next - No need to test this one */
/**
 *
 * @param {THREE.PerspectiveCamera} camera The camera
 * @param {number} near Near distance
 * @param {number} far Far distance
 * @returns {THREE.Matrix4} projectionMatrix
 */
export function createModifiedProjectionMatrix(
    camera: THREE.PerspectiveCamera,
    near: number,
    far: number
): THREE.Matrix4 {
    const modifiedCamera = camera.clone();
    modifiedCamera.near = near;
    modifiedCamera.far = far;
    modifiedCamera.updateProjectionMatrix();
    return modifiedCamera.projectionMatrix;
}
