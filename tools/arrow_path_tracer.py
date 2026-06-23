import cv2
import numpy as np
import json
import sys
import os


def get_cell_size(v_peaks, h_peaks):
    nv, nh = len(v_peaks), len(h_peaks)
    cell_w = (v_peaks[-1] - v_peaks[0]) / max(nv - 1, 1)
    cell_h = (h_peaks[-1] - h_peaks[0]) / max(nh - 1, 1)
    return cell_w, cell_h


def detect_grid(img):
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    _, binary = cv2.threshold(gray, 120, 255, cv2.THRESH_BINARY_INV)

    vertical = cv2.reduce(binary, 0, cv2.REDUCE_AVG).flatten()
    horizontal = cv2.reduce(binary, 1, cv2.REDUCE_AVG).flatten()

    v_peaks, h_peaks = [], []
    v_thresh = np.max(vertical) * 0.4
    h_thresh = np.max(horizontal) * 0.4

    for i in range(1, len(vertical) - 1):
        if vertical[i] > v_thresh and vertical[i] > vertical[i-1] and vertical[i] >= vertical[i+1]:
            v_peaks.append(i)

    for i in range(1, len(horizontal) - 1):
        if horizontal[i] > h_thresh and horizontal[i] > horizontal[i-1] and horizontal[i] >= horizontal[i+1]:
            h_peaks.append(i)

    if len(v_peaks) < 2 or len(h_peaks) < 2:
        h_peaks = np.linspace(0, img.shape[0] - 1, 6, dtype=int).tolist()
        v_peaks = np.linspace(0, img.shape[1] - 1, 6, dtype=int).tolist()
        print(f"  Using fallback grid: {len(v_peaks)}x{len(h_peaks)}")

    return v_peaks, h_peaks, binary


def pixel_to_grid(px, py, v_peaks, h_peaks):
    cell_w = (v_peaks[-1] - v_peaks[0]) / max(len(v_peaks) - 1, 1)
    cell_h = (h_peaks[-1] - h_peaks[0]) / max(len(h_peaks) - 1, 1)
    if cell_w == 0 or cell_h == 0:
        return 0, 0
    gx = round((px - v_peaks[0]) / cell_w)
    gy = round((py - h_peaks[0]) / cell_h)
    gx = int(max(0, min(gx, len(v_peaks) - 1)))
    gy = int(max(0, min(gy, len(h_peaks) - 1)))
    return gx, gy


def grid_to_pixel(gx, gy, v_peaks, h_peaks):
    cell_w = (v_peaks[-1] - v_peaks[0]) / max(len(v_peaks) - 1, 1)
    cell_h = (h_peaks[-1] - h_peaks[0]) / max(len(h_peaks) - 1, 1)
    px = int(v_peaks[0] + gx * cell_w + cell_w / 2)
    py = int(h_peaks[0] + gy * cell_h + cell_h / 2)
    return px, py


def find_arrow_heads(binary_img):
    contours, _ = cv2.findContours(binary_img, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)

    heads = []
    for cnt in contours:
        area = cv2.contourArea(cnt)
        if area < 40 or area > 5000:
            continue

        hull = cv2.convexHull(cnt)
        hull_area = cv2.contourArea(hull)
        if hull_area == 0:
            continue
        solidity = area / hull_area

        if solidity < 0.4 or solidity > 0.97:
            continue

        x, y, w, h = cv2.boundingRect(cnt)
        cx, cy = x + w // 2, y + h // 2

        moments = cv2.moments(cnt)
        if moments["mu20"] + moments["mu02"] == 0:
            continue
        orientation = 0.5 * np.arctan2(
            2 * moments["mu11"],
            moments["mu20"] - moments["mu02"]
        )
        angle_deg = np.degrees(orientation)

        vx = np.cos(orientation)
        vy = np.sin(orientation)

        # figure out which way the arrow points (not just orientation, but direction)
        # project contour points onto the orientation axis to find the "tip" side
        pts = cnt[:, 0, :].astype(np.float32)
        proj = pts @ np.array([vx, vy])
        tip_idx = np.argmax(proj)
        tip_pt = pts[tip_idx]
        tail_idx = np.argmin(proj)
        tail_pt = pts[tail_idx]

        # the tip should be more pointed than the tail
        # check if the tip has more curvature (smaller angle)
        # compute angle at tip point
        def angle_at_point(contour_pts, idx):
            n = len(contour_pts)
            p0 = contour_pts[(idx - 1) % n][0]
            p1 = contour_pts[idx][0]
            p2 = contour_pts[(idx + 1) % n][0]
            v1 = p0 - p1
            v2 = p2 - p1
            norm1 = np.linalg.norm(v1)
            norm2 = np.linalg.norm(v2)
            if norm1 == 0 or norm2 == 0:
                return 180
            cos_a = np.clip(np.dot(v1, v2) / (norm1 * norm2), -1, 1)
            return np.degrees(np.arccos(cos_a))

        n = len(cnt)
        tip_angle = angle_at_point(cnt, tip_idx)
        tail_angle = angle_at_point(cnt, tail_idx)

        # direction: from tail toward tip
        if tip_angle < tail_angle:
            # tip is pointed -> head direction is from center toward tip
            dx, dy = tip_pt[0] - cx, tip_pt[1] - cy
        else:
            # tail is pointed (unlikely, but handle it)
            dx, dy = cx - tail_pt[0], cy - tail_pt[1]

        norm = np.linalg.norm([dx, dy])
        if norm > 0:
            dx, dy = dx / norm, dy / norm

        if abs(dx) > abs(dy):
            if dx > 0:
                direction = "right"
                ddx, ddy = 1, 0
            else:
                direction = "left"
                ddx, ddy = -1, 0
        else:
            if dy > 0:
                direction = "down"
                ddx, ddy = 0, 1
            else:
                direction = "up"
                ddx, ddy = 0, -1

        heads.append({
            "pixel": (cx, cy),
            "bbox": (x, y, w, h),
            "dir": direction,
            "dir_vec": (ddx, ddy),
            "area": area,
            "solidity": solidity,
            "contour": cnt
        })

    # merge nearby duplicates
    merged = []
    used = set()
    for i, a in enumerate(heads):
        if i in used:
            continue
        group = [a]
        used.add(i)
        for j, b in enumerate(heads):
            if j in used:
                continue
            ax, ay = a["pixel"]
            bx, by = b["pixel"]
            if abs(ax - bx) < 15 and abs(ay - by) < 15:
                group.append(b)
                used.add(j)
        best = max(group, key=lambda x: x["area"])
        merged.append(best)

    return merged


def trace_body(binary_img, head_px, dir_vec, v_peaks, h_peaks, max_steps=50):
    hx, hy = head_px
    ddx, ddy = dir_vec

    # body is opposite to head direction
    trace_dx = -ddx
    trace_dy = -ddy

    cell_w, cell_h = get_cell_size(v_peaks, h_peaks)
    step_dist = max(1, int(round(min(cell_w, cell_h) * 0.5)))

    img_h, img_w = binary_img.shape[:2]
    path_pixels = [(hx, hy)]

    for _ in range(max_steps):
        cx, cy = path_pixels[-1]
        found = False

        # try straight direction at cell-sized step
        search_offsets = [-2, -1, 0, 1, 2]
        for offset in search_offsets:
            dist = max(1, int(round(step_dist + offset)))
            nx = int(round(cx + trace_dx * dist))
            ny = int(round(cy + trace_dy * dist))
            if 0 <= nx < img_w and 0 <= ny < img_h and binary_img[ny, nx] > 0:
                pt = (nx, ny)
                if pt not in path_pixels:
                    path_pixels.append(pt)
                    found = True
                    break

        if found:
            continue

        # try perpendicular (90° turn)
        for perp_sign in [-1, 1]:
            perp_dx = -trace_dy * perp_sign
            perp_dy = trace_dx * perp_sign
            for offset in search_offsets:
                dist = max(1, int(round(step_dist + offset)))
                nx = int(round(cx + perp_dx * dist))
                ny = int(round(cy + perp_dy * dist))
                if 0 <= nx < img_w and 0 <= ny < img_h and binary_img[ny, nx] > 0:
                    pt = (nx, ny)
                    if pt not in path_pixels:
                        path_pixels.append(pt)
                        trace_dx, trace_dy = perp_dx, perp_dy
                        found = True
                        break
            if found:
                break

        if not found:
            break

    return path_pixels


def path_to_grid_cells(path_pixels, v_peaks, h_peaks):
    grid_cells = []
    seen = set()
    for px, py in path_pixels:
        gx, gy = pixel_to_grid(px, py, v_peaks, h_peaks)
        key = (gx, gy)
        if key not in seen:
            seen.add(key)
            grid_cells.append([gx, gy])
    return grid_cells


def draw_debug(img, v_peaks, h_peaks, arrows_data):
    debug = img.copy()
    overlay = debug.copy()
    cell_w, cell_h = get_cell_size(v_peaks, h_peaks)

    for x in v_peaks:
        cv2.line(overlay, (x, 0), (x, img.shape[0]), (100, 100, 100), 1)
    for y in h_peaks:
        cv2.line(overlay, (0, y), (img.shape[1], y), (100, 100, 100), 1)

    cv2.addWeighted(overlay, 0.5, debug, 0.5, 0, debug)

    colors = [
        (0, 0, 255), (0, 255, 0), (255, 0, 0), (255, 255, 0),
        (255, 0, 255), (0, 255, 255), (128, 0, 255), (0, 128, 255),
        (255, 128, 0), (128, 255, 0)
    ]

    for i, ad in enumerate(arrows_data):
        color = colors[i % len(colors)]
        cells = ad["cells"]

        for j, cell in enumerate(cells):
            gx, gy = cell
            px = int(v_peaks[0] + gx * cell_w + cell_w / 2)
            py = int(h_peaks[0] + gy * cell_h + cell_h / 2)

            if j == 0:
                cv2.drawMarker(debug, (px, py), color, cv2.MARKER_TRIANGLE_UP, 20, 2)
                cv2.putText(debug, str(i + 1), (px + 10, py - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)
            else:
                cv2.circle(debug, (px, py), 6, color, -1)

            if j > 0:
                pgx, pgy = cells[j - 1]
                ppx = int(v_peaks[0] + pgx * cell_w + cell_w / 2)
                ppy = int(h_peaks[0] + pgy * cell_h + cell_h / 2)
                cv2.line(debug, (ppx, ppy), (px, py), color, 3)

        x, y, w, h = ad["bbox"]
        cv2.rectangle(debug, (x, y), (x + w, y + h), color, 1)

    return debug


def main():
    if len(sys.argv) < 2:
        print("Usage: python arrow_path_tracer.py <image_path> [--debug]")
        print("  Reads arrow puzzle image, traces full head->body path, exports JSON.")
        print("  Format: cells[0] = HEAD, cells[1:] = body")
        sys.exit(1)

    image_path = sys.argv[1]
    show_debug = "--debug" in sys.argv

    if not os.path.exists(image_path):
        print(f"Error: Image not found: {image_path}")
        sys.exit(1)

    img = cv2.imread(image_path)
    if img is None:
        print(f"Error: Could not load image: {image_path}")
        sys.exit(1)

    print(f"Processing: {image_path} ({img.shape[1]}x{img.shape[0]})")

    v_peaks, h_peaks, binary = detect_grid(img)
    grid_w, grid_h = len(v_peaks), len(h_peaks)
    print(f"Grid: {grid_w}x{grid_h} cells")

    heads = find_arrow_heads(binary)
    print(f"Arrow heads found: {len(heads)}")

    arrows_data = []
    for head in heads:
        cx, cy = head["pixel"]
        gx, gy = pixel_to_grid(cx, cy, v_peaks, h_peaks)

        path_px = trace_body(binary, (cx, cy), head["dir_vec"], v_peaks, h_peaks)
        cells = path_to_grid_cells(path_px, v_peaks, h_peaks)

        if len(cells) < 2:
            cells = [[gx, gy]]
            for step in range(1, 4):
                ngx = gx - head["dir_vec"][0] * step
                ngy = gy - head["dir_vec"][1] * step
                if 0 <= ngx < grid_w and 0 <= ngy < grid_h:
                    cells.append([ngx, ngy])

        arrow_entry = {
            "head": [gx, gy],
            "cells": cells,
            "dir": head["dir"],
            "length": len(cells),
            "bbox": [int(v) for v in head["bbox"]]
        }
        arrows_data.append(arrow_entry)

        head_cell = f"({gx},{gy})"
        body_cells = "->".join([f"({c[0]},{c[1]})" for c in cells[1:]])
        print(f"  #{len(arrows_data)} HEAD={head_cell} ({head['dir']}) body={body_cells}")

    level_json = {
        "grid": f"{grid_w}x{grid_h}",
        "moves": max(len(arrows_data) + 3, 5),
        "arrows": []
    }

    for ad in arrows_data:
        level_json["arrows"].append({
            "cells": ad["cells"]
        })

    base = os.path.splitext(image_path)[0]
    json_path = base + "_arrows.json"
    with open(json_path, "w") as f:
        json.dump(level_json, f, indent=2)
    print(f"\nSaved: {json_path}")

    cells_only_path = base + "_cells.json"
    cells_output = {"arrows": [{"cells": ad["cells"]} for ad in arrows_data]}
    with open(cells_only_path, "w") as f:
        json.dump(cells_output, f, indent=2)
    print(f"Saved: {cells_only_path}")

    print(f"\nFORMAT: cells[0] = HEAD, cells[1:] = body")
    print(f"Grid: {grid_w}x{grid_h}  Arrows: {len(arrows_data)}  Moves: {level_json['moves']}")
    print("Copy JSON into Unity -> Window -> Arrow Puzzle Level Importer (JSON mode)")

    if show_debug:
        debug_img = draw_debug(img, v_peaks, h_peaks, arrows_data)
        debug_path = base + "_debug.png"
        cv2.imwrite(debug_path, debug_img)
        print(f"Debug: {debug_path}")


if __name__ == "__main__":
    main()
