struct Float3 {
    x: f32,
    y: f32,
    z: f32,
}

#[unsafe(no_mangle)]
pub extern "C" fn returnMyStruct() -> *mut Float3 {
    let elt = Float3 {
        x: 51.0,
        y: 64.0,
        z: 90.0,
    };

    let elt_on_heap = Box::new(elt);

    Box::into_raw(elt_on_heap)
}

#[unsafe(no_mangle)]
pub extern "C" fn deleteMyStruct(elt: *mut Float3) {
    unsafe {
        let _ = Box::from_raw(elt);
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn return42() -> i32 {
    42
}

#[unsafe(no_mangle)]
pub extern "C" fn my_add(a: i32, b: i32) -> i32 {
    a + b
}

#[unsafe(no_mangle)]
pub extern "C" fn compute_targets(
    source_positions_array: *mut Float3,
    source_positions_array_length: i32,
    target_positions_array: *mut Float3,
    target_positions_array_length: i32,
    result_indices: *mut i32,
) {
    let (source_positions_array, target_positions_array, result_indices) = unsafe {
        (
            std::slice::from_raw_parts(source_positions_array, source_positions_array_length as usize),
            std::slice::from_raw_parts(target_positions_array, target_positions_array_length as usize),
            std::slice::from_raw_parts_mut(result_indices, source_positions_array_length as usize),
        )
    };

    for (i, sourcePos) in source_positions_array.iter().enumerate() {
        let mut best_index = 0;
        let mut best_squared_distance = f32::MAX;

        for (j, targetPos) in target_positions_array.iter().enumerate() {
            let squared_distance =
                (sourcePos.x - targetPos.x).powf(2.0) +
                    (sourcePos.y - targetPos.y).powf(2.0) +
                    (sourcePos.z - targetPos.z).powf(2.0);
            if squared_distance < best_squared_distance {
                best_index = j;
                best_squared_distance = squared_distance;
            }
        }

        result_indices[i] = best_index as i32
    }
}
