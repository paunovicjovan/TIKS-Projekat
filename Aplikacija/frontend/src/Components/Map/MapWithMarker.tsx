import { useEffect, useState } from "react";
import { Icon } from "leaflet";
import { MapContainer, Marker, TileLayer, useMap } from "react-leaflet";

import MapMarker from "../Map/MapMarker";
import { GeoCoordinates } from "../../Interfaces/GeoCoordinates/GeoCoordinates ";

import locationPin from "../../assets/location-pin.png";

interface Props {
    setLat?: (latitude: number | null) => void;
    setLong?: (longitude: number | null) => void;
    lat: number | null;
    long: number | null;
}

const MapWithMarker: React.FC<Props & {editMode?: boolean}> = ({ setLat, setLong, long, lat, editMode = true }) => {
    const [location, setLocation] = useState<GeoCoordinates | null>(null);
    const [mapCenter, setMapCenter] = useState<{ lat: number; lng: number }>({
        lat: 43.32083030,
        lng: 21.89544071
    });

    const customIcon = new Icon({
        iconUrl: locationPin,
        iconSize: [38, 38],
    });

    useEffect(() => {
        if (long !== null && lat !== null) {
            const newLocation: GeoCoordinates = { latitude: lat, longitude: long };
            setLocation(newLocation);
            setMapCenter({ lat, lng: long });
        }
    }, []);

    useEffect(() => {
        if (location) {
            setLong?.(location.longitude);
            setLat?.(location.latitude);
        }
    }, [location, setLat, setLong]);

    return (
        <div>
            <div className={`container pb-5`}>
                <MapContainer
                    center={mapCenter}
                    zoom={14}
                    style={{ width: '100%', height: '500px' }}
                    zoomControl={false}
                    attributionControl={false}
                >
                    <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                    <MapMarker setLocation={setLocation} editMode={editMode} />
                    <ChangeMapCenter center={mapCenter} />
                    {location != null && lat != null && long != null && (
                        <Marker
                            position={{ lat: location.latitude, lng: location.longitude }}
                            icon={customIcon}
                        />
                    )}
                </MapContainer>
            </div>
        </div>
    );
};

const ChangeMapCenter: React.FC<{ center: { lat: number; lng: number } }> = ({ center }) => {
    const map = useMap();

    useEffect(() => {
        map.setView(center);
    }, [center, map]);
    
    return null;
};

export default MapWithMarker;