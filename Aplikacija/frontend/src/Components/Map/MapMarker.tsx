import React from "react";
import { useMapEvents } from "react-leaflet";
import { LeafletMouseEvent } from "leaflet";
import {GeoCoordinates} from "../../Interfaces/GeoCoordinates/GeoCoordinates "
interface Props {
    setLocation: (loc: GeoCoordinates | null) => void;
    editMode: boolean;
}

const MapMarker: React.FC<Props> = ({ setLocation, editMode }) => {
    useMapEvents({
        click: (e: LeafletMouseEvent) => {
            if(!editMode) return;
            
            const { lat, lng } = e.latlng;
            const newLocation: GeoCoordinates = { latitude: lat, longitude: lng };            
            setLocation(newLocation);
        }
    });

    return null;
}

export default MapMarker;