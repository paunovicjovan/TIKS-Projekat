import {ChangeEvent, useEffect, useState} from "react";
import {
  Pagination as MaterialPagination,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  SelectChangeEvent
} from "@mui/material";

type Props = {
  totalLength: number;
  onPaginateChange: (page: number, pageSize: number) => void;
  currentPage?: number;
  perPage?:number;
};

export const Pagination = ({ totalLength, onPaginateChange, currentPage, perPage}: Props) => {
  const [page, setPage] = useState<number>(currentPage ?? 1);
  const [pageSize, setPageSize] = useState<number>(perPage ?? 10);

  const pageCount = Math.ceil(totalLength / pageSize);

  useEffect(() => {
    onPaginateChange(page, pageSize);
  }, [page, pageSize]);

  useEffect(() => {
    setPage(currentPage ?? 1);
    setPageSize(perPage ?? 10);
  }, [currentPage, perPage]);

  const handlePageChange = (_: ChangeEvent<unknown>, value: number) => {
    setPage(value);
  };

  const handlePageSizeChange = (event: SelectChangeEvent<number>) => {
    setPageSize(event.target.value as number);
    setPage(1);
  };

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "10px", justifyContent: "center", marginTop: "10px" }}>
      <MaterialPagination count={pageCount} page={page} onChange={handlePageChange} color="standard" />

      <FormControl variant="outlined" size="small">
        <InputLabel shrink>Po stranici</InputLabel>
        <Select value={pageSize} onChange={handlePageSizeChange} label="Po stranici">
          <MenuItem value={5}>5</MenuItem>
          <MenuItem value={10}>10</MenuItem>
          <MenuItem value={20}>20</MenuItem>
        </Select>
      </FormControl>
    </div>
  );
};